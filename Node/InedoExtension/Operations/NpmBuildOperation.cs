namespace Inedo.Extension.Node.Operations;

[DisplayName("npm Build")]
[Description("Runs the `npm run build` command.")]
[ScriptAlias("Build")]
[ScriptNamespace("npm")]
public sealed class NpmBuildOperation : NpmOperation
{
    [ScriptAlias("AdditionalArguments")]
    [DisplayName("Additional Command line arguments")]
    public string? AdditionalArguments { get; set; }

    public override Task ExecuteAsync(IOperationExecutionContext context)
        => this.ExecuteNpmAsync("run build", this.AdditionalArguments, context);

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        return new ExtendedRichDescription(
            new RichDescription(
                "npm build on ",
                new DirectoryHilite(config[nameof(SourceDirectory)])
            )
        );
    }
}
