using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Estud.Back.Filters;

/// <summary>
/// Esconde da documentação (Swagger, Scalar e OpenApi) todo endpoint cuja rota comece com "admin/".
/// </summary>
public class HideAdminFromDocsConvention : IActionModelConvention
{
    private const string AdminPrefix = "admin/";

    public void Apply(ActionModel action)
    {
        var controllerTemplate = action.Controller.Selectors
            .Select(x => x.AttributeRouteModel?.Template)
            .FirstOrDefault(x => x.HasValue());

        var isAdmin = action.Selectors.Any(x =>
        {
            var route = AttributeRouteModel.CombineTemplates(
                controllerTemplate, x.AttributeRouteModel?.Template);

            return route.HasValue() && route!.StartsWith(AdminPrefix, StringComparison.OrdinalIgnoreCase);
        });

        if (isAdmin) action.ApiExplorer.IsVisible = false;
    }
}
