using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMount;

public sealed record ReleaseInfo(Version? Version, Uri? DownloadUrl, bool IsPublished);

public sealed class ReleaseClient
{
    public static readonly Uri Website = new("https://easymount-cloud-drive.trtqt1203.chatgpt.site/");
    private readonly HttpClient client;
    private readonly Uri endpoint;
    public ReleaseClient(HttpClient client, Uri? endpoint = null)
    {
        this.client = client;
        this.endpoint = endpoint ?? new Uri(Website, "api/releases");
        if (this.endpoint.Scheme != "https" && !(this.endpoint.IsLoopback && this.endpoint.Scheme == "http"))
            throw new ArgumentException("Update metadata requires HTTPS.");
    }
    public async Task<ReleaseInfo> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.ParseAdd("EasyMount/0.4");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions { MaxDepth = 12 }, cancellationToken);
        var root = document.RootElement;
        if (root.TryGetProperty("status", out var status) && status.GetString() == "unpublished") return new(null, null, false);
        if (!root.TryGetProperty("version", out var version) || !System.Version.TryParse(version.GetString(), out var parsed)
            || !root.TryGetProperty("downloadUrl", out var url) || !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var download)
            || download.Scheme != "https" || download.Host != "github.com" || !download.AbsolutePath.StartsWith("/trtqt1203/EasyMount/releases/download/", StringComparison.Ordinal)
            || !download.AbsolutePath.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Thông tin bản cập nhật không hợp lệ.");
        return new(parsed, download, true);
    }
}
