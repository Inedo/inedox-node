using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extension.Node.VariableFunctions;

[Undisclosed]
[ScriptAlias("NpmPath")]
[Description("The path to npm/npm.cmd.")]
[ExtensionConfigurationVariable(Required = false)]
public sealed class NpmPathVariableFunction : ScalarVariableFunction
{
    protected override object EvaluateScalar(IVariableFunctionContext context) => string.Empty;
}
