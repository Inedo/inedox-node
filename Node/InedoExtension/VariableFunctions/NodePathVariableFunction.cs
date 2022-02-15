using Inedo.Extensibility.VariableFunctions;

namespace Inedo.Extension.Node.VariableFunctions;

[Undisclosed]
[ScriptAlias("NodePath")]
[Description("The path to node/node.exe.")]
[ExtensionConfigurationVariable(Required = false)]
public sealed class NodePathVariableFunction : ScalarVariableFunction
{
    protected override object EvaluateScalar(IVariableFunctionContext context) => string.Empty;
}
