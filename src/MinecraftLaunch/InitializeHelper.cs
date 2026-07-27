using MinecraftLaunch.Base.Models;
using MinecraftLaunch.Utilities;

namespace MinecraftLaunch;

public static class InitializeHelper {
    public static void Initialize(Action<ComponentSettings> settingsProvider) {
        var componentSettings = new ComponentSettings();
        settingsProvider(componentSettings);

        DownloadManager.MaxThread = componentSettings.MaxThread;
        DownloadManager.MaxFragment = componentSettings.MaxFragment;
        DownloadManager.IsEnableMirror = componentSettings.IsEnableMirror;
        DownloadManager.IsEnableFragment = componentSettings.IsEnableFragment;
        DownloadManager.CurseforgeApiKey = componentSettings.CurseForgeApiKey;

        HttpUtil.Configure(componentSettings.DisableSystemProxy, componentSettings.ProxyServer, componentSettings.UserAgent);
    }
}
