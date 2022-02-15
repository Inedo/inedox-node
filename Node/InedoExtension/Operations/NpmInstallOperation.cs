namespace Inedo.Extension.Node.Operations;

[DisplayName("npm Install")]
[Description("Runs the npm install command.")]
[ScriptAlias("Install")]
[ScriptNamespace("npm")]
public sealed class NpmInstallOperation : NpmOperation
{
    [ScriptAlias("AdditionalArguments")]
    [DisplayName("Additional Command line arguments")]
    public string? AdditionalArguments { get; set; }

    public override Task ExecuteAsync(IOperationExecutionContext context) 
        => this.ExecuteNpmAsync("install", this.AdditionalArguments, context);

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        return new ExtendedRichDescription(
            new RichDescription(
                "npm install on ",
                new DirectoryHilite(config[nameof(SourceDirectory)])
            )
        );
    }
}
