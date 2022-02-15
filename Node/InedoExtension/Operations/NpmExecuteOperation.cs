namespace Inedo.Extension.Node.Operations
{
    [DisplayName("npm Execute Command")]
    [Description("Runs the specified npm command (ex: `npm rebuild`).")]
    [ScriptAlias("Execute-Command")]
    [ScriptNamespace("npm")]
    [Example(@"# Run the `npm audit fix` command
npm::Execute-Command audit
(
    Arguments: fix
);")]
    public sealed class NpmExecuteOperation : NpmOperation
    {
        [NotNull]
        [Required]
        [ScriptAlias("Command")]
        [DisplayName("Command")]
        [Description("The npm command to execute.")]
        public string? Command { get; set; }

        [ScriptAlias("Arguments")]
        [DisplayName("Command line arguments")]
        public string? Arguments { get; set; }

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            if (string.IsNullOrEmpty(this.Command))
            {
                this.LogError("Command is required.");
                return Complete;
            }

            return this.ExecuteNpmAsync(this.Command, this.Arguments, context);
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "npm ",
                    new Hilite(config[nameof(this.Command)]),
                    " ",
                    new DirectoryHilite(config[nameof(SourceDirectory)])
                )
            );
        }
    }
}
