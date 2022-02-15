namespace Inedo.Extension.Node.Operations;

[DisplayName("npm Run")]
[Description("Runs the specified npm run command (ex: `npm run lessc`).")]
[ScriptAlias("Run")]
[ScriptNamespace("npm")]
[DefaultProperty(nameof(Command))]
[Example(@"# Run the lessc command from npm
npm::Run lessc
(
    SourceDirectory: ~\styles
);")]
public sealed class NpmRunOperation : NpmOperation
{
    [NotNull]
    [Required]
    [ScriptAlias("Command")]
    [DisplayName("Command")]
    [Description("The command to execute using npm run.")]
    public string? Command { get; set; }

    [ScriptAlias("AdditionalArguments")]
    [DisplayName("Additional Command line arguments")]
    public string? AdditionalArguments { get; set; }

    public override Task ExecuteAsync(IOperationExecutionContext context)
    {
        if (string.IsNullOrEmpty(this.Command))
        {
            this.LogError("Command is required.");
            return Complete;
        }

        return this.ExecuteNpmAsync("run", $"{this.Command} {this.AdditionalArguments}", context);
    }

    protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
    {
        return new ExtendedRichDescription(
            new RichDescription(
                "npm run ",
                new Hilite(config[nameof(Command)]), 
                " on ",
                new DirectoryHilite(config[nameof(SourceDirectory)])
            )
        );
    }
}
