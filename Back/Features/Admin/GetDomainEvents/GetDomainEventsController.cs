namespace Estud.Back.Features.Admin.GetDomainEvents;

[ApiController, Authorize(Policies.GetDomainEvents)]
public class GetDomainEventsController(GetDomainEventsService service) : ControllerBase
{
    /// <summary>
    /// Listar eventos de domínio
    /// </summary>
    /// <remarks>
    /// Lista paginada de eventos de domínio, atravessando todos os tenants, com filtro por status,
    /// tipo, instituição, entidade e janela de tempo. Ordenação: mais recente primeiro. O filtro
    /// por status=Error é o caso de uso principal: ver o que falhou no processamento.
    /// </remarks>
    [HttpGet("admin/domain-events")]
    public async Task<IActionResult> Get([FromQuery] GetDomainEventsIn query)
    {
        var result = await service.Get(query);
        return Ok(result);
    }
}
