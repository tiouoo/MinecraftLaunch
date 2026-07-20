using Flurl;
using Flurl.Http;

namespace MinecraftLaunch.Utilities;

public static class HttpUtil {
    internal static HttpClient DownloaderClient { get; } = new();
    // Installers are public APIs and must work even when the host has not supplied custom settings.
    public static IFlurlClient FlurlClient { get; internal set; } = new FlurlClient();

    public static IFlurlRequest Request(Url url) {
        return FlurlClient.Request(url);
    }

    public static IFlurlRequest Request(string url) {
        return FlurlClient.Request(url);
    }

    public static IFlurlRequest Request(Url baseUrl, params string[] paths) {
        return FlurlClient.Request(baseUrl.AppendPathSegments(paths));
    }
}
