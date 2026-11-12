using System.Net;

namespace DannyGoodacre.Identity.Tests.Infrastructure;

internal static class HttpAssert
{
    /// <summary>
    /// Asserts the status code, and shows the response body on failure.
    /// </summary>
    public static void AssertStatus(HttpStatusCode expected, HttpResponseMessage response)
        => Assert.True(
            expected == response.StatusCode,
            $"Expected {(int)expected} {expected}, got {(int)response.StatusCode} {response.StatusCode}: "
            + response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
}
