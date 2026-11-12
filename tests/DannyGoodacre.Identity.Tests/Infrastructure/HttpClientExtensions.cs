using System.Net;
using System.Net.Http.Json;

namespace DannyGoodacre.Identity.Tests.Infrastructure;

internal static class HttpClientExtensions
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    extension(HttpClient client)
    {
        public Task<HttpResponseMessage> Get(string path)
            => client.GetAsync(path, CancellationToken);

        public Task<HttpResponseMessage> Post(string path, object? body = null)
            => client.PostAsJsonAsync(path, body ?? new { }, CancellationToken);

        public Task<HttpResponseMessage> Put(string path, object? body = null)
            => client.PutAsJsonAsync(path, body ?? new { }, CancellationToken);

        public Task<HttpResponseMessage> Delete(string path)
            => client.DeleteAsync(path, CancellationToken);

        public async Task<T> GetJson<T>(string path)
        {
            var response = await client.GetAsync(path, CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<T>(CancellationToken))!;
        }
    }
}
