namespace Estud.Back.Features.Institutions.GetInstitutionNoteTypes;

[ApiController, Authorize(Policies.GetInstitutionNoteTypes)]
public class GetInstitutionNoteTypesController(GetInstitutionNoteTypesService service) : ControllerBase
{
    /// <summary>
    /// Tipos de nota da instituição
    /// </summary>
    /// <remarks>
    /// Retorna os tipos de nota usados pela instituição do usuário logado, de acordo com a regra de
    /// cálculo de média configurada.
    /// </remarks>
    [HttpGet("institutions/note-types")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Get()
    {
        var noteTypes = await service.Get();
        return Ok(noteTypes);
    }
}

internal class ResponseExamples : ExamplesProvider<GetInstitutionNoteTypesOut>;
