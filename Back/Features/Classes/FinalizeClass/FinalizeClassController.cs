namespace Estud.Back.Features.Classes.FinalizeClass;

[ApiController, Authorize(Policies.FinalizeClass)]
public class FinalizeClassController(FinalizeClassService service) : ControllerBase
{
    /// <summary>
    /// Finalizar turma
    /// </summary>
    /// <remarks>
    /// Finaliza a turma ao final do semestre, quando todas as notas e frequências já foram lançadas.
    /// A turma deve estar iniciada. Após finalizada, não é possível retroceder.
    /// </remarks>
    [HttpPut("classes/{classId}/finalize")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Finalize([FromRoute] int classId)
    {
        var result = await service.Finalize(classId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ClassNotFound,
    ClassAlreadyFinalized,
    ClassMustBeStarted
>;
