using CmlLib.Core.Files;
using CmlLib.Core.Java;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;

namespace CmlLib.Core.Test.Version;

public class MockVersion : IVersion
{
    public MockVersion(string id) => Id = id;

    public string Id { get; set; }

    public string? InheritsFrom { get; set; }

    public IVersion? ParentVersion { get; set; }

    public AssetMetadata? AssetIndex { get; set; }

    public MFileMetadata? Client { get; set; }

    public JavaVersion? JavaVersion { get; set; }

    public MLibrary[] Libraries { get; set; } = new MLibrary[0];

    public string? Jar { get; set; }

    public MLogFileMetadata? Logging { get; set; }

    public string? MainClass { get; set; }

    public MArgument[] GameArguments { get; set; } = new MArgument[0];

    public MArgument[] JvmArguments { get; set; } = new MArgument[0];

    public DateTime ReleaseTime { get; set; }

    public string? Type { get; set; }

    public string? JarId { get; set; }

    public string? GetProperty(string key)
    {
        throw new NotImplementedException();
    }
}