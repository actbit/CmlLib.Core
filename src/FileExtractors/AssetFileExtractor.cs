﻿using CmlLib.Core.Files;
using CmlLib.Core.Internals;
using CmlLib.Core.Rules;
using CmlLib.Core.Tasks;
using CmlLib.Core.Version;
using System.Diagnostics;
using System.Text.Json;

namespace CmlLib.Core.FileExtractors;

public class AssetFileExtractor : IFileExtractor
{
    private readonly HttpClient httpClient;

    public AssetFileExtractor(HttpClient client)
    {
        this.httpClient = client;
    }

    public string AssetServer { get; set; } = MojangServer.ResourceDownload;

    public async ValueTask<IEnumerable<GameFile>> Extract(
        MinecraftPath path,
        IVersion version,
        RulesEvaluatorContext rulesContext,
        CancellationToken cancellationToken)
    {
        var assetIndex = await loadAssetIndex(path, version.AssetIndex);
        if (assetIndex == null)
            return Enumerable.Empty<GameFile>();

        return TaskExtractor.ExtractTasksFromAssetIndex(
            assetIndex, 
            path, 
            AssetServer,
            dispose: true);
    }

    private async ValueTask<IAssetIndex?> loadAssetIndex(MinecraftPath path, MFileMetadata? assets)
    {
        if (assets == null ||
            string.IsNullOrEmpty(assets?.Id))
            return null;

        using var assetIndexStream = await createAssetIndexStream(path, assets);
        if (assetIndexStream == null)
            return null;

        var assetIndexJson = await JsonDocument.ParseAsync(assetIndexStream);
        return new JsonAssetIndex(assets.Id, assetIndexJson);
    }

    private async ValueTask<Stream?> createAssetIndexStream(MinecraftPath path, MFileMetadata assets)
    {
        Debug.Assert(!string.IsNullOrEmpty(assets.Id));

        var indexFilePath = path.GetIndexFilePath(assets.Id);
        if (IOUtil.CheckFileValidation(indexFilePath, assets.Sha1))
        {
            return File.Open(indexFilePath, FileMode.Open);
        }
        else
        {
            if (string.IsNullOrEmpty(assets.Url))
                return null;

            IOUtil.CreateParentDirectory(indexFilePath);

            var ms = new MemoryStream();
            using (var res = await httpClient.GetStreamAsync(assets.Url))
            {
                await res.CopyToAsync(ms);
            } // dispose immediately

            ms.Position = 0;
            using (var fs = File.Create(indexFilePath))
            {
                await ms.CopyToAsync(fs);
            } // dispose immediately

            ms.Position = 0;
            return ms;
        }
    }

    public static class TaskExtractor
    {
        public static IEnumerable<GameFile> ExtractTasksFromAssetIndex(
            IAssetIndex assetIndex, MinecraftPath path, string assetServer, bool dispose)
        {
            if (assetServer.Last() != '/')
                assetServer += '/';

            foreach (var assetObject in assetIndex.EnumerateAssetObjects())
            {
                var hashName = assetObject.Hash.Substring(0, 2) + "/" + assetObject.Hash;
                var hashPath = Path.Combine(path.GetAssetObjectPath(assetIndex.Id), hashName);

                var copyPath = new List<string>(2);
                if (assetIndex.IsVirtual)
                    copyPath.Add(Path.Combine(path.GetAssetLegacyPath(assetIndex.Id), assetObject.Name));
                if (assetIndex.MapToResources)
                    copyPath.Add(Path.Combine(path.Resource, assetObject.Name));

                var file = new GameFile(assetObject.Name)
                {
                    Path = hashPath,
                    Hash = assetObject.Hash,
                    Size = assetObject.Size,
                    Url = assetServer + hashName
                };

                if (copyPath.Any())
                    file.UpdateTask = new FileCopyTask(copyPath);

                yield return file;
            }

            if (dispose && assetIndex is IDisposable disposableAssetIndex)
                disposableAssetIndex.Dispose();
        }
    }
}