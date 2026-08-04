namespace Estud.Back.Features.Classes.ReleaseClassForEnrollment;

[ApiController, Authorize(Policies.ReleaseClassForEnrollment)]
public class ReleaseClassForEnrollmentController(ReleaseClassForEnrollmentService service) : ControllerBase
{
    /// <summary>
    /// Liberar turma para matrícula
    /// </summary>
    /// <remarks>
    /// Libera a turma para que os alunos possam se matricular.
    /// Só é possível dentro do período de matrícula vigente, e a turma deve estar em pré-matrícula.
    /// </remarks>
    [HttpPut("classes/{classId}/release-for-enrollment")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Release(int classId)
    {
        var result = await service.Release(classId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ClassNotFound,
    ClassMustBeOnPreEnrollment
>;
