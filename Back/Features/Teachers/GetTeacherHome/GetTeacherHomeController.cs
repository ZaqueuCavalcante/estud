namespace Estud.Back.Features.Teachers.GetTeacherHome;

[ApiController, Authorize(Policies.GetTeacherHome)]
public class GetTeacherHomeController(GetTeacherHomeService service) : ControllerBase
{
    /// <summary>
    /// Home do professor
    /// </summary>
    /// <remarks>
    /// Retorna os dados da página inicial do professor logado: os indicadores gerais
    /// (turmas ativas e alunos) e as turmas que ele leciona, exceto as em pré-matrícula e as finalizadas.
    /// </remarks>
    [HttpGet("teachers/home")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Get()
    {
        var data = await service.Get();
        return Ok(data);
    }
}

internal class ResponseExamples : ExamplesProvider<GetTeacherHomeOut>;
