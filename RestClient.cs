using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Klinkby.RestClient
{
    public class RestClient : IDisposable
    {
        private const string JsonMimeType = "application/json";
        private readonly bool _httpClientOwned;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializer _serializer;
        private bool _disposedValue = false; // To detect redundant calls

        public RestClient(JsonSerializer serializer = null, string mime = null, HttpClient httpClient = null)
        {
            _serializer = serializer ?? JsonSerializer.CreateDefault();
            _httpClientOwned = null == httpClient;
            _httpClient = httpClient ?? CreateDefaultClient(mime ?? JsonMimeType);
        }

        public async Task<TResult> GetAsync<TResult>(Uri endPoint, IDictionary<string, string> urlParams, CancellationToken ct)
        {
            if (null == endPoint) throw new ArgumentNullException(nameof(endPoint));
            endPoint = AddParams(endPoint, urlParams);
            Debug.Write($"GET {endPoint}");
            using (var res = await _httpClient.GetAsync(endPoint, ct))
            {
                return await ParseResultAsync<TResult>(res);
            }
        }

        public async Task PostAsync(Uri endPoint, object body, IDictionary<string, string> urlParams, CancellationToken ct)
        {
            if (null == endPoint) throw new ArgumentNullException(nameof(endPoint));
            endPoint = AddParams(endPoint, urlParams);
            Debug.Write($"POST {endPoint}");
            await WithStreamContentAsync(body, async sc =>
            {
                using (var res = await _httpClient.PostAsync(endPoint, sc, ct))
                {
                    EnsureSuccessStatusCode(res);
                }
            });
        }

        public async Task<TResult> PostAsync<TResult>(Uri endPoint, object body, IDictionary<string, string> urlParams, CancellationToken ct)
        {
            if (null == endPoint) throw new ArgumentNullException(nameof(endPoint));
            endPoint = AddParams(endPoint, urlParams);
            Debug.Write($"POST {endPoint}");
            TResult result = default(TResult);
            await WithStreamContentAsync(body, async sc =>
            {
                using (var res = await _httpClient.PostAsync(endPoint, sc, ct))
                {
                    result = await ParseResultAsync<TResult>(res);
                }
            });
            return result;
        }

        public async Task PutAsync(Uri endPoint, object body, IDictionary<string, string> urlParams, CancellationToken ct)
        {
            if (null == endPoint) throw new ArgumentNullException(nameof(endPoint));
            endPoint = AddParams(endPoint, urlParams);
            Debug.Write($"PUT {endPoint}");
            await WithStreamContentAsync(body, async sc =>
            {
                using (var res = await _httpClient.PutAsync(endPoint, sc, ct))
                {
                    EnsureSuccessStatusCode(res);
                }
            });
        }

        public async Task<TResult> PutAsync<TResult>(Uri endPoint, object body, IDictionary<string, string> urlParams, CancellationToken ct)
        {
            if (null == endPoint) throw new ArgumentNullException(nameof(endPoint));
            endPoint = AddParams(endPoint, urlParams);
            Debug.Write($"PUT {endPoint}");
            TResult result = default(TResult);
            await WithStreamContentAsync(body, async sc =>
            {
                using (var res = await _httpClient.PutAsync(endPoint, sc, ct))
                {
                    result = await ParseResultAsync<TResult>(res);
                }
            });
            return result;
        }

        public async Task DeleteAsync(Uri endPoint, IDictionary<string, string> urlParams, CancellationToken ct)
        {
            if (null == endPoint) throw new ArgumentNullException(nameof(endPoint));
            endPoint = AddParams(endPoint, urlParams);
            Debug.Write($"DELETE {endPoint}");
            using (var res = await _httpClient.DeleteAsync(endPoint, ct))
            {
                EnsureSuccessStatusCode(res);
            }
        }

        private async Task WithStreamContentAsync(object body, Func<StreamContent, Task> withStreamContent)
        {
            using (var ms = new MemoryStream())
            {
                var jw = new JsonTextWriter(new StreamWriter(ms));
                _serializer.Serialize(jw, body);
                jw.Flush();
                Debug.Write($" {ms.Length} bytes");
                ms.Seek(0, SeekOrigin.Begin);
                using (var sc = new StreamContent(ms))
                {
                    sc.Headers.ContentType = new MediaTypeHeaderValue(JsonMimeType);
                    await withStreamContent(sc);
                }
            }
        }

        private static void EnsureSuccessStatusCode(HttpResponseMessage res)
        {
            Debug.WriteLine($" ({res.StatusCode})");
            res.EnsureSuccessStatusCode();
        }

        private static HttpClient CreateDefaultClient(string mime)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mime));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("KlinkbyRestClient", "1.0"));
            return client;
        }

        private async Task<TResult> ParseResultAsync<TResult>(HttpResponseMessage res)
        {
            EnsureSuccessStatusCode(res);
            using (var stream = await res.Content.ReadAsStreamAsync())
            {
                Debug.WriteLine($"  Parse {stream.Length} bytes");
                var jr = new JsonTextReader(new StreamReader(stream));
                return _serializer.Deserialize<TResult>(jr);
            }
        }

        private static Uri AddParams(Uri endPoint, IDictionary<string, string> urlParams)
        {
            if (null != urlParams)
            {
                var paramsJoined = string.Join(
                    "&",
                    urlParams
                    .Where(x => !string.IsNullOrEmpty(x.Key))
                    .Select(x =>
                        Uri.EscapeDataString(x.Key)
                        + "="
                        + Uri.EscapeDataString(x.Value ?? "")
                    ).ToArray()
                );
                endPoint = new Uri(
                    endPoint.AbsoluteUri
                    + (string.IsNullOrEmpty(endPoint.Query) ? "?" : "&")
                    + paramsJoined
                );
            }
            return endPoint;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _httpClientOwned)
                {
                    _httpClient.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
