namespace Estud.Back.Features.Admin.GetInstitutions;

[ApiController, Authorize(Policies.GetInstitutions)]
public class GetInstitutionsController(GetInstitutionsService service) : ControllerBase
{
    /// <summary>
    /// Listar instituições
    /// </summary>
    /// <remarks>
    /// Lista paginada de instituições, atravessando todos os tenants, com filtro por nome e a
    /// contagem de usuários de cada instituição. Ordenação: mais recente primeiro.
    /// </remarks>
    [HttpGet("admin/institutions")]
    public async Task<IActionResult> Get([FromQuery] GetInstitutionsIn query)
    {
        var result = await service.Get(query);
        return Ok(result);
    }
}
