namespace Estud.Back.Features.Students.GenerateEnrollmentProof;

[ApiController, Authorize(Policies.GenerateEnrollmentProof)]
public class GenerateEnrollmentProofController(GenerateEnrollmentProofService service) : ControllerBase
{
    /// <summary>
    /// Gerar comprovante de matrícula
    /// </summary>
    /// <remarks>
    /// Gera o comprovante de matrícula do aluno logado em PDF, referente à sua oferta de curso ativa.
    /// O documento traz um código de verificação e um QR Code que apontam para a validação pública online.
    /// O PDF não é armazenado — apenas o registro de verificação é persistido.
    /// </remarks>
    [HttpPost("students/enrollment-proofs")]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Generate()
    {
        var result = await service.Generate();
        return result.Match<IActionResult>(
            file => File(file.Content, "application/pdf", file.FileName),
            BadRequest
        );
    }
}

internal class ErrorsExamples : ErrorExamplesProvider<StudentNotFound, StudentNotEnrolledInAnyCourse>;
