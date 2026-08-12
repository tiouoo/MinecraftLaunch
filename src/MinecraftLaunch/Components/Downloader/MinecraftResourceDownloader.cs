using Flurl.Http;
using MinecraftLaunch.Base.EventArgs;
using MinecraftLaunch.Base.Interfaces;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Base.Models.Network;
using MinecraftLaunch.Extensions;
using MinecraftLaunch.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

namespace MinecraftLaunch.Components.Downloader;

public sealed class MinecraftResourceDownloader {
    private readonly MinecraftEntry _entry;
    private readonly DefaultDownloader _downloader;
    private readonly List<MinecraftDependency> _dependencies = [];

    public event EventHandler<ResourceDownloadProgressChangedEventArgs> ProgressChanged;

    internal int TotalCount { get; set; }
    public bool AllowVerifyAssets { get; init; } = true;
    public bool AllowInheritedDependencies { get; init; } = true;
    
    public IEnumerable<string> SourceRootDirectories { get; set; } = [];

    public MinecraftResourceDownloader(MinecraftEntry entry, IEnumerable<MinecraftDependency> extraDependencies = null) {
        if (extraDependencies is not null)
            _dependencies.AddRange(extraDependencies);

        _entry = entry;
        _downloader = new();
    }

    public async Task<GroupDownloadResult> VerifyAndDownloadDependenciesAsync(int fileVerificationParallelism = 10, CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fileVerificationParallelism);

        #region 1.1 Libraries & Inherited Libraries

        var (libs, nativeLibs) = _entry.GetRequiredLibraries();
        _dependencies.AddRange(libs);
        _dependencies.AddRange(nativeLibs);

        if (AllowInheritedDependencies
            && _entry is ModifiedMinecraftEntry modInstance
            && modInstance.HasInheritance) {
            (libs, nativeLibs) = modInstance.InheritedMinecraft.GetRequiredLibraries();
            _dependencies.AddRange(libs);
            _dependencies.AddRange(nativeLibs);
        }

        #endregion

        #region 1.2 Client.jar

        var jar = _entry.GetJarElement();
        if (jar != null) {
            _dependencies.Add(jar);
        }

        #endregion

        #region 1.3 AssetIndex & Assets

        if (AllowVerifyAssets) {
            var assetIndex = _entry.GetAssetIndex();

            // 验证 AssetIndex 文件
            if (!VerifyDependency(assetIndex, cancellationToken)) {
                var assetIndexDirectory = Path.Combine(assetIndex.MinecraftFolderPath, "assets", "indexes");
                Directory.CreateDirectory(assetIndexDirectory);
                await using var assetIndexStream = await HttpUtil.Request(assetIndex.Url)
                    .GetStreamAsync(HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                await using var assetIndexFile = File.Create(Path.Combine(assetIndexDirectory, $"{assetIndex.Id}.json"));
                await assetIndexStream.CopyToAsync(assetIndexFile, cancellationToken);
            }

            // 添加资源文件到依赖列表
            _dependencies.AddRange(_entry.GetRequiredAssets());
        }

        #endregion

        // 2. 验证依赖项
        ConcurrentBag<MinecraftDependency> invalidDeps = [];
        Parallel.ForEach(_dependencies, new ParallelOptions
        {
            MaxDegreeOfParallelism = fileVerificationParallelism,
            CancellationToken = cancellationToken
        }, dep => {
            if (!VerifyDependency(dep, cancellationToken)) {
                invalidDeps.Add(dep);
            }
        });

        ConcurrentBag<MinecraftDependency> dependenciesToDownload = [];
        Parallel.ForEach(invalidDeps, new ParallelOptions
        {
            MaxDegreeOfParallelism = fileVerificationParallelism,
            CancellationToken = cancellationToken
        }, dep => {
            if (dep is IDownloadDependency && TryCopyDependencyFromSources(dep, cancellationToken))
                return;
            dependenciesToDownload.Add(dep);
        });

        TotalCount = dependenciesToDownload.Count;
        var downloadItems = dependenciesToDownload
            .OfType<IDownloadDependency>()
            .Select(dep => new DownloadRequest(dep.Url, dep.FullPath, dep.Size ?? 0))
            .ToList();

        Debug.WriteLine(_dependencies.Where(x => x is FabricLibrary).Count());

        var groupDownloadRequest = new GroupDownloadRequest(downloadItems);
        groupDownloadRequest.ProgressChanged += args
            => ProgressChanged?.Invoke(this, args);

        return await _downloader.DownloadManyAsync(groupDownloadRequest, cancellationToken);
    }

    #region Privates

    private static bool VerifyDependency(MinecraftDependency dep, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        Debug.WriteLineIf(dep is FabricLibrary, dep.FullPath);
        if (!File.Exists(dep.FullPath))
            return false;

        if (dep is not IVerifiableDependency verifiableDependency)
            return true;

        return VerifyFileContent(dep.FullPath, verifiableDependency);
    }
    
    private bool TryCopyDependencyFromSources(MinecraftDependency dependency, CancellationToken cancellationToken = default) {
        if (dependency is not IVerifiableDependency verifiableDependency)
            return false;

        foreach (var sourceRoot in SourceRootDirectories) {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(sourceRoot))
                continue;

            var candidate = Path.Combine(sourceRoot, dependency.FilePath);
            if (!File.Exists(candidate))
                continue;

            if (!VerifyFileContent(candidate, verifiableDependency))
                continue;

            var target = dependency.FullPath;
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(candidate, target, true);
                return true;
            } catch (Exception exception) {
                Debug.WriteLine($"从本地资源目录复制文件失败：{candidate} -> {target}{Environment.NewLine}{exception}");
            }
        }
        return false;
    }

    private static bool VerifyFileContent(string filePath, IVerifiableDependency verifiableDependency) {
        if (verifiableDependency.Sha1 is { } sha1) {
            using var fileStream = File.OpenRead(filePath);
            var sha1Bytes = (Span<byte>)stackalloc byte[20];
            SHA1.HashData(fileStream, sha1Bytes);
            return sha1Bytes.SequenceEqual(sha1);
        }

        if (verifiableDependency.Size is long size)
            return new FileInfo(filePath).Length == size;

        return false;
    }

    #endregion
}
