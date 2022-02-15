using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inedo.Agents;
using Inedo.IO;

namespace Inedo.Extension.Node.Operations;


[DisplayName("npm Set Project Version")]
[Description("Sets the version in an npm package.json.")]
[ScriptAlias("Set-ProjectVersion")]
[ScriptNamespace("npm")]
public sealed class NpmSetProjectVersionOperation : ExecuteOperation
{
    [NotNull]
    [ScriptAlias("Version")]
    [DisplayName("Version")]
    [DefaultValue("$ReleaseNumber")]
    public string? Version { get; set; }

    [ScriptAlias("SourceDirectory")]
    [DisplayName("Source directory")]
    [PlaceholderText("$WorkingDirectory")]
    public string? SourceDirectory { get; set; }

    public override async Task ExecuteAsync(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(this.Version))
        {
            this.LogError("Version is required.");
            return;
        }

        var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>();
        var sourceDirectory = context.ResolvePath(this.SourceDirectory ?? context.WorkingDirectory);
        var packageJsonPath = PathEx.Combine(sourceDirectory, "package.json");
        if (!await fileOps.FileExistsAsync(packageJsonPath))
        {
            this.LogError("package.json not found");
            return;
        }
        this.LogDebug($"Found package.json at {packageJsonPath}");
        JsonObject? projectJson;
        using (var packageStream = await fileOps.OpenFileAsync(packageJsonPath, fileMode: FileMode.Open, fileAccess: FileAccess.Read))
        {
            projectJson = await JsonSerializer.DeserializeAsync<JsonObject>(packageStream, cancellationToken: context.CancellationToken);
        }
        if (projectJson == null)
        {
            this.LogError("package.json could not be deserialized.");
            return;
        }

        this.LogInformation($"Setting package version to {this.Version}");
        projectJson["version"] = this.Version;

        fileOps.WriteAllText(packageJsonPath, JsonSerializer.Serialize(projectJson), InedoLib.UTF8Encoding);
        this.LogDebug("Updated package version");
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        return new ExtendedRichDescription(
            new RichDescription(
                "Set npm Project Version to ",
                new Hilite(config[nameof(Version)])
            ),
            new RichDescription(
                "in ",
                new DirectoryHilite(config[nameof(SourceDirectory)])
            )
        );
    }
}
