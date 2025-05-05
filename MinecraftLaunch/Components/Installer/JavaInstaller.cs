using Flurl.Http;
using MinecraftLaunch.Base.Enums;
using MinecraftLaunch.Base.Interfaces;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Base.Models.Network;
using MinecraftLaunch.Components.Downloader;
using MinecraftLaunch.Components.Parser;
using MinecraftLaunch.Extensions;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MinecraftLaunch.Components.Installer;

/// <summary>
/// Java 通用安装器
/// </summary>
public sealed class JavaInstaller : InstallerBase
{
    public string JavaVersion { get; init; }
    public string InstallPath { get; init; }
    public override string MinecraftFolder { get; init; }

    public static JavaInstaller Create(string installPath, string javaVersion, string minecraftFolder)
    {
        return new JavaInstaller
        {
            JavaVersion = javaVersion,
            InstallPath = installPath,
            MinecraftFolder = minecraftFolder
        };
    }

    /// <summary>
    /// 异步安装 Java 环境。
    /// 警告：该函数因不是安装 Minecraft 内容，因此将不会返回 MinecraftEntry 对象。
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>NULL</returns>
    /// 
    public override async Task<MinecraftEntry> InstallAsync(CancellationToken cancellationToken = default)
    {
        FileInfo javaPackageFile = default;

        ReportProgress(InstallStep.Started, 0.0d, TaskStatus.WaitingToRun, 1, 1);

        try
        {
            javaPackageFile = await DownloadJavaPackageAsync(cancellationToken);
            await ExtractJavaPackageAsync(javaPackageFile, cancellationToken);
            ValidateJavaInstallation();

            ReportProgress(InstallStep.RanToCompletion, 1.0d, TaskStatus.RanToCompletion, 1, 1);
            ReportCompleted();
        }
        catch (Exception)
        {
            ReportProgress(InstallStep.Interrupted, 1.0d, TaskStatus.Canceled, 1, 1);
            ReportCompleted();
            throw;
        }

        return null; // JavaInstaller 不返回 MinecraftEntry
    }

    #region Privates

    /// <summary>
    /// 下载 Java 安装包。
    /// </summary>
    private async Task<FileInfo> DownloadJavaPackageAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(InstallStep.DownloadPackage, 0.10d, TaskStatus.Running, 1, 0);

        string packageUrl = $"https://api.adoptopenjdk.net/v3/binary/latest/{JavaVersion}/ga/windows/x64/jdk/hotspot/normal/adoptopenjdk";
        string fileName = $"java-{JavaVersion}.zip";
        var packageFile = new FileInfo(Path.Combine(InstallPath, fileName));

        var downloadRequest = new DownloadRequest(packageUrl, packageFile.FullName);
        await new FileDownloader(DownloadMirrorManager.MaxThread)
            .DownloadFileAsync(downloadRequest, cancellationToken);

        ReportProgress(InstallStep.DownloadPackage, 0.30d, TaskStatus.Running, 1, 1);
        return packageFile;
    }

    /// <summary>
    /// 解压 Java 安装包。
    /// </summary>
    private async Task ExtractJavaPackageAsync(FileInfo javaPackageFile, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReportProgress(InstallStep.ParsePackage, 0.30d, TaskStatus.Running, 1, 0);

        string extractPath = Path.Combine(InstallPath, $"java-{JavaVersion}");
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        await Task.Run(() => {
            ZipFile.ExtractToDirectory(javaPackageFile.FullName, extractPath);
        }, cancellationToken);

        ReportProgress(InstallStep.ParsePackage, 0.60d, TaskStatus.Running, 1, 1);
    }

    /// <summary>
    /// 验证 Java 安装是否成功。
    /// </summary>
    private void ValidateJavaInstallation()
    {
        ReportProgress(InstallStep.RunInstallProcessor, 0.60d, TaskStatus.Running, 1, 0);

        string javaExecutable = Path.Combine(InstallPath, $"java-{JavaVersion}", "bin", "java.exe");
        if (!File.Exists(javaExecutable))
        {
            throw new FileNotFoundException("Java executable not found after installation.");
        }

        var process = Process.Start(new ProcessStartInfo(javaExecutable)
        {
            Arguments = "-version",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process == null)
        {
            throw new Exception("Failed to start Java process for validation.");
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception("Java installation validation failed.");
        }

        ReportProgress(InstallStep.RunInstallProcessor, 1.0d, TaskStatus.RanToCompletion, 1, 1);
    }

    #endregion
}
