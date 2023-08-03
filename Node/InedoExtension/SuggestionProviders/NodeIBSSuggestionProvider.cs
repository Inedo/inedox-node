using System.Collections.Immutable;
using Inedo.Web;

namespace Inedo.Extensions.DotNet.SuggestionProviders;

internal sealed class NodeIBSSuggestionProvider : ImageBasedServiceSuggestionProvider
{
    protected override ImmutableArray<string> RequiredCapabilities => ImmutableArray.Create("node");
}
