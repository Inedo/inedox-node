namespace Inedo.Extension.Node.Operations;

[DisplayName("npm Publish")]
[Description("Runs the npm publish command.")]
[ScriptAlias("Publish")]
[ScriptNamespace("npm")]
public sealed class NpmPublishOperation : NpmOperation
{
    [ScriptAlias("AdditionalArguments")]
    [DisplayName("Additional Command line arguments")]
    public string? AdditionalArguments { get; set; }

    public override Task ExecuteAsync(IOperationExecutionContext context) =>
        this.ExecuteNpmAsync("publish", this.AdditionalArguments, context);

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        return new ExtendedRichDescription(
            new RichDescription(
                "npm publish on ",
                new DirectoryHilite(config[nameof(SourceDirectory)])
            )
        );
    }
}
