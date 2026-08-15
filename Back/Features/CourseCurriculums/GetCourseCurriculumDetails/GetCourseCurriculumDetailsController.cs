namespace Estud.Back.Features.CourseCurriculums.GetCourseCurriculumDetails;

[ApiController, Authorize(Policies.GetCourseCurriculumDetails)]
public class GetCourseCurriculumDetailsController(GetCourseCurriculumDetailsService service) : ControllerBase
{
    /// <summary>
    /// Detalhes da grade curricular
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes de uma grade curricular, incluindo o curso, as disciplinas com período,
    /// créditos e carga horária, os totais da grade e as ofertas que a utilizam.
    /// </remarks>
    [HttpGet("course-curriculums/{courseCurriculumId}/details")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int courseCurriculumId)
    {
        var result = await service.Get(courseCurriculumId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetCourseCurriculumDetailsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CourseCurriculumNotFound>;
