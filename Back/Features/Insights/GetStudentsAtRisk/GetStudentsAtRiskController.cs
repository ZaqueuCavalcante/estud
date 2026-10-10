namespace Estud.Back.Features.Insights.GetStudentsAtRisk;

[ApiController, Authorize(Policies.GetStudentsAtRisk)]
public class GetStudentsAtRiskController(GetStudentsAtRiskService service) : ControllerBase
{
    /// <summary>
    /// Alunos em risco de reprovação
    /// </summary>
    /// <remarks>
    /// Retorna quantos alunos matriculados em turmas do período acadêmico informado estão,
    /// em ao menos uma turma, com frequência ou nota média abaixo da mínima da instituição.
    /// </remarks>
    [HttpGet("insights/students/at-risk")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromQuery] GetStudentsAtRiskIn data)
    {
        var result = await service.Get(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetStudentsAtRiskIn>;
internal class ResponseExamples : ExamplesProvider<GetStudentsAtRiskOut>;
internal class ErrorsExamples : ErrorExamplesProvider<AcademicPeriodNotFound>;
