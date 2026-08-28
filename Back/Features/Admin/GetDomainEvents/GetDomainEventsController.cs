namespace Estud.Back.Features.Admin.GetDomainEvents;

[ApiController, Authorize(Policies.GetDomainEvents)]
public class GetDomainEventsController(GetDomainEventsService service) : ControllerBase
{
    /// <summary>
    /// Listar eventos de domínio
    /// </summary>
    /// <remarks>
    /// Lista paginada de eventos de domínio, atravessando todos os tenants.
    /// </remarks>
    [HttpGet("admin/domain-events")]
    public async Task<IActionResult> Get([FromQuery] GetDomainEventsIn query)
    {
        var result = await service.Get(query);
        return Ok(result);
    }
}
