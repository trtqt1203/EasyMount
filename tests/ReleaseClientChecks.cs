using System.Net;
using System.Net.Http;
using EasyMount;

internal static class ReleaseClientChecks
{
    public static async Task RunAsync(bool live)
    {
        using var unpublished = new HttpClient(new Handler("{\"status\":\"unpublished\",\"version\":null,\"downloadUrl\":null}"));
        if ((await new ReleaseClient(unpublished).CheckAsync()).IsPublished) throw new Exception("Unpublished metadata misreported.");
        Console.WriteLine("PASS update client distinguishes an unpublished release");
        using var valid = new HttpClient(new Handler("{\"version\":\"0.4.0\",\"downloadUrl\":\"https://github.com/trtqt1203/EasyMount/releases/download/v0.4.0/EasyMount-0.4.0-Setup.exe\"}"));
        if ((await new ReleaseClient(valid).CheckAsync()).Version != new Version("0.4.0")) throw new Exception("Version parse failed.");
        Console.WriteLine("PASS update client accepts an official installer asset");
        foreach (var target in new[] { "http://github.com/trtqt1203/EasyMount/releases/download/v1/Setup.exe", "https://evil.example/Setup.exe", "https://github.com/other/repo/releases/download/v1/Setup.exe" })
        {
            using var bad = new HttpClient(new Handler(System.Text.Json.JsonSerializer.Serialize(new { version = "0.4.0", downloadUrl = target })));
            try { await new ReleaseClient(bad).CheckAsync(); throw new Exception("Unsafe update target accepted."); }
            catch (InvalidOperationException) { Console.WriteLine("PASS update client rejects an untrusted installer target"); }
        }
        using var broken = new HttpClient(new Handler("{}", HttpStatusCode.ServiceUnavailable));
        try { await new ReleaseClient(broken).CheckAsync(); throw new Exception("Service error accepted."); }
        catch (HttpRequestException) { Console.WriteLine("PASS update client reports an unavailable backend"); }
        if (live)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var endpoint = Environment.GetEnvironmentVariable("EASYMOUNT_TEST_API_URL");
            var result = await new ReleaseClient(http, string.IsNullOrWhiteSpace(endpoint)
                ? new Uri(ReleaseClient.Website, "api/releases") : new Uri(endpoint)).CheckAsync();
            Console.WriteLine("PASS desktop .NET client consumes live website backend; published=" + result.IsPublished);
        }
    }
    private sealed class Handler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
