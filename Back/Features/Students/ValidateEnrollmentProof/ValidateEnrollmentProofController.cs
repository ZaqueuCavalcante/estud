namespace Estud.Back.Features.Students.ValidateEnrollmentProof;

[ApiController]
public class ValidateEnrollmentProofController(ValidateEnrollmentProofService service) : ControllerBase
{
    /// <summary>
    /// Validar comprovante de matrícula 🔓
    /// </summary>
    /// <remarks>
    /// Endpoint público. Confere a autenticidade de um comprovante de matrícula a partir do seu código de
    /// verificação, retornando os dados oficiais registrados no momento da emissão.
    /// </remarks>
    [HttpPost("students/enrollment-proofs/{code}/validate")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Validate(string code)
    {
        var result = await service.Validate(code);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<ValidateEnrollmentProofOut>;
internal class ErrorsExamples : ErrorExamplesProvider<EnrollmentProofNotFound>;
