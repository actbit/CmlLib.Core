using CmlLib.Core.Tasks;

namespace CmlLib.Core.Installers;

public interface IGameInstaller
{
    ValueTask Install(
        IReadOnlyList<GameFile> gameFiles,
        IProgress<InstallerProgressChangedEventArgs>? fileProgress,
        IProgress<ByteProgress>? byteProgress,
        CancellationToken cancellationToken);
}