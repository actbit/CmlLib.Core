using System.Text.Json;
using CmlLib.Core.Internals;
using CmlLib.Core.VersionMetadata;

namespace CmlLib.Core.VersionLoader;

public class MojangJsonVersionLoaderV2 : IVersionLoader
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly LocalJsonVersionLoader _localLoader;
    private readonly string _localManifestPath;

    public MojangJsonVersionLoaderV2(MinecraftPath path, HttpClient httpClient) :
        this(path, httpClient, MojangServer.VersionV2)
    {

    }

    public MojangJsonVersionLoaderV2(MinecraftPath path, HttpClient httpClient, string endpoint)
    {
        _httpClient = httpClient;
        _endpoint = endpoint;
        _localLoader = new LocalJsonVersionLoader(path);
        _localManifestPath = Path.Combine(path.Versions, "version_manifest_v2.json");
    }

    public bool UseLocalManifestWhenError { get; set; } = false;

    public async ValueTask<VersionMetadataCollection> GetVersionMetadatasAsync()
    {
        var localVersions = _localLoader.GetVersionNameAndPaths();
        var localVersionDict = localVersions.ToDictionary(nameAndPath => nameAndPath.Item1, nameAndPath => nameAndPath.Item2);
        var mojangVersions = await getManifest();

        var vanillaVersions = new List<IVersionMetadata>();
        foreach (var mojangVersion in mojangVersions?.Versions ?? [])
        {
            string id;
            if (!string.IsNullOrEmpty(mojangVersion.Id))
                id = mojangVersion.Id;
            else if (!string.IsNullOrEmpty(mojangVersion.Name))
                id = mojangVersion.Name;
            else
                continue;

            if (localVersionDict.TryGetValue(id, out var localVersion))
            {
                localVersionDict.Remove(id);
                var valid = IOUtil.CheckFileValidation(localVersion, mojangVersion.Sha1);
                if (valid)
                {
                    vanillaVersions.Add(new LocalVersionMetadata(mojangVersion, localVersion));
                }
                else
                {
                    vanillaVersions.Add(new MojangVersionMetadata(mojangVersion, _httpClient));
                }
            }
            else
            {
                vanillaVersions.Add(new MojangVersionMetadata(mojangVersion, _httpClient));
            }
        }

        var remainLocalVersions = localVersionDict
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .Select(nameAndPath =>
            {
                var model = new JsonVersionMetadataModel
                {
                    Id = nameAndPath.Key,
                    Type = "local"
                };
                return new LocalVersionMetadata(model, nameAndPath.Value);
            });

        // 1. custom local versions
        // 2. vanilla versions
        return new VersionMetadataCollection(
            remainLocalVersions.Concat(vanillaVersions),
            mojangVersions?.Latest?.Release,
            mojangVersions?.Latest?.Snapshot
        );
    }

    private async Task<JsonVersionManifestModel?> getManifest()
    {
        using var stream = await getManifestStream();
        return await JsonSerializer.DeserializeAsync<JsonVersionManifestModel>(stream);
    }

    private async Task<Stream> getManifestStream()
    {
        try
        {
            using var res = await _httpClient.GetStreamAsync(_endpoint);
            var buffer = new MemoryStream();
            await res.CopyToAsync(buffer);

            using var saveTo = File.Create(_localManifestPath);
            await buffer.CopyToAsync(saveTo);

            buffer.Position = 0;
            return buffer;
        }
        catch (HttpRequestException)
        {
            if (!UseLocalManifestWhenError)
                throw;

            return File.OpenRead(_localManifestPath);
        }
    }
}