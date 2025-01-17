using System.IO.Abstractions;
using JetBrains.Annotations;

namespace Recyclarr.Migration.Steps;

/// <summary>
///     Rename `trash.yml` to `recyclarr.yml`.
/// </summary>
/// <remarks>
///     Implemented on 4/30/2022.
/// </remarks>
[UsedImplicitly]
public class MigrateTrashYml : IMigrationStep
{
    private readonly IFileSystem _fileSystem;
    private readonly string _oldConfigPath = Path.Combine(AppContext.BaseDirectory, "trash.yml");

    // Do not use AppPaths class here since that may change yet again in the future and break this migration step.
    private readonly string _newConfigPath = Path.Combine(AppContext.BaseDirectory, "recyclarr.yml");

    public int Order => 10;
    public string Description { get; }
    public IReadOnlyCollection<string> Remediation { get; }

    public MigrateTrashYml(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        Remediation = new[]
        {
            $"Check if `{_newConfigPath}` already exists. If so, manually copy the data you want and then delete `{_oldConfigPath}` to fix the error.",
            $"Ensure Recyclarr has permission to delete {_oldConfigPath}",
            $"Ensure Recyclarr has permission to create {_newConfigPath}"
        };

        Description = $"Migration from `{_oldConfigPath}` to `{_newConfigPath}`";
    }

    public bool CheckIfNeeded() => _fileSystem.File.Exists(_oldConfigPath);

    public void Execute()
    {
        _fileSystem.File.Move(_oldConfigPath, _newConfigPath);
    }
}
