using CmlLib.Core.Files;
using CmlLib.Core.Java;
using CmlLib.Core.ProcessBuilder;

namespace CmlLib.Core.Version;

public interface IVersion
{
    string Id { get; }
    string? InheritsFrom { get; }
    IVersion? ParentVersion { get; set; }
    AssetMetadata? AssetIndex { get; }
    MFileMetadata? Client { get; }
    JavaVersion? JavaVersion { get; }
    MLibrary[] Libraries { get; }
    string? Jar { get; }
    MLogFileMetadata? Logging { get; }
    string? MainClass { get; }
    MArgument[] GameArguments { get; }
    MArgument[] JvmArguments { get; }
    DateTime ReleaseTime { get; }
    string? Type { get; }

    string? GetProperty(string key);
}