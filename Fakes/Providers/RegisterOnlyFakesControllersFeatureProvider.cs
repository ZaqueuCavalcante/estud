using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Estud.Fakes.Providers;

public sealed class RegisterOnlyFakesControllersFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        for (int i = feature.Controllers.Count - 1; i >= 0; i--)
        {
            var ctrl = feature.Controllers[i];

            if (!ctrl.AssemblyQualifiedName.StartsWith("Estud.Fakes"))
                feature.Controllers.RemoveAt(i);
        }
    }
}
