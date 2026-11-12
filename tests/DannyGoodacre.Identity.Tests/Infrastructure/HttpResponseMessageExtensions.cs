using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DannyGoodacre.Identity.Tests.Infrastructure;

internal static class HttpResponseMessageExtensions
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    extension(HttpResponseMessage response)
    {
        public Task<T?> ReadAsync<T>()
            => response.Content.ReadFromJsonAsync<T>(CancellationToken);

        public async Task<ProblemDetails> ReadProblemAsync()
            => (await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken))!;

        public async Task<HttpValidationProblemDetails> ReadValidationProblemAsync()
            => (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(CancellationToken))!;
    }
}
