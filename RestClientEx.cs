using System;
using System.Threading;
using System.Threading.Tasks;

namespace Klinkby.RestClient
{
    public static class RestClientEx
    {
        public static Task<TResult> GetAsync<TResult>(this RestClient instance, Uri endPoint, CancellationToken ct)
        {
            return instance.GetAsync<TResult>(endPoint, null, ct); 
        }

        public static Task<TResult> PostAsync<TResult>(this RestClient instance, Uri endPoint, object body, CancellationToken ct)
        {
            return instance.PostAsync<TResult>(endPoint, body, null, ct);
        }

        public static Task PostAsync(this RestClient instance, Uri endPoint, object body, CancellationToken ct)
        {
            return instance.PostAsync(endPoint, body, null, ct);
        }

        public static Task PutAsync(this RestClient instance, Uri endPoint, object body, CancellationToken ct)
        {
            return instance.PutAsync(endPoint, body, null, ct); 
        }
        public static Task<TResult> PutAsync<TResult>(this RestClient instance, Uri endPoint, object body, CancellationToken ct)
        {
            return instance.PutAsync<TResult>(endPoint, body, null, ct); 
        }

        public static Task DeleteAsync(this RestClient instance, Uri endPoint, CancellationToken ct)
        {
            return instance.DeleteAsync(endPoint, null, ct);
        }
    }
}
