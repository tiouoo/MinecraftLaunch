using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using System.Net;

namespace MinecraftLaunch.Utilities;

public static class HttpUtil {
    private static HttpClient _downloaderClient = CreateClient(false, null, "MinecraftLaunch/4.0");
    private static IFlurlClient _flurlClient = CreateFlurlClient(false, null, "MinecraftLaunch/4.0");

    internal static HttpClient DownloaderClient => _downloaderClient;
    // Installers are public APIs and must work even when the host has not supplied custom settings.
    public static IFlurlClient FlurlClient => _flurlClient;
    public static HttpClient Client => _downloaderClient;

    public static void Configure(bool disableSystemProxy, string proxyServer, string userAgent) {
        var newDownloaderClient = CreateClient(disableSystemProxy, proxyServer, userAgent);
        var newFlurlClient = CreateFlurlClient(disableSystemProxy, proxyServer, userAgent);
        Interlocked.Exchange(ref _downloaderClient, newDownloaderClient);
        Interlocked.Exchange(ref _flurlClient, newFlurlClient);
    }

    public static IFlurlRequest Request(Url url) {
        return FlurlClient.Request(url);
    }

    public static IFlurlRequest Request(string url) {
        return FlurlClient.Request(url);
    }

    public static IFlurlRequest Request(Url baseUrl, params string[] paths) {
        return FlurlClient.Request(baseUrl.AppendPathSegments(paths));
    }

    private static IFlurlClient CreateFlurlClient(bool disableSystemProxy, string proxyServer, string userAgent) {
        return new FlurlClient(CreateClient(disableSystemProxy, proxyServer, userAgent)) {
            Settings = {
                Timeout = TimeSpan.FromSeconds(15),
                JsonSerializer = new DefaultJsonSerializer(JsonSerializerUtil.GetDefaultOptions()),
                Redirects = { Enabled = true }
            }
        };
    }

    private static HttpClient CreateClient(bool disableSystemProxy, string proxyServer, string userAgent) {
        var hasProxyServer = TryGetProxyUri(proxyServer, out var proxyUri);
        var handler = new SocketsHttpHandler {
            UseProxy = !disableSystemProxy || hasProxyServer
        };
        if (hasProxyServer) handler.Proxy = new WebProxy(proxyUri);
        var client = new HttpClient(handler);
        if (!string.IsNullOrWhiteSpace(userAgent)) client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        return client;
    }

    private static bool TryGetProxyUri(string proxyServer, out Uri proxyUri) {
        if (!string.IsNullOrWhiteSpace(proxyServer) && !proxyServer.Contains("://", StringComparison.Ordinal))
            proxyServer = $"http://{proxyServer}";
        return Uri.TryCreate(proxyServer, UriKind.Absolute, out proxyUri);
    }
}
