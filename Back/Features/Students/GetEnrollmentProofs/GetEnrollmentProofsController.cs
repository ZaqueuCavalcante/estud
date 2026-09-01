namespace Estud.Back.Features.Students.GetEnrollmentProofs;

[ApiController, Authorize(Policies.GetEnrollmentProofs)]
public class GetEnrollmentProofsController(GetEnrollmentProofsService service) : ControllerBase
{
    /// <summary>
    /// Comprovantes de matrícula
    /// </summary>
    /// <remarks>
    /// Lista todos os comprovantes de matrícula emitidos pelo aluno logado, do mais recente para o mais antigo.
    /// Cada item traz o código de verificação e a data de emissão.
    /// </remarks>
    [HttpGet("students/enrollment-proofs")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Get()
    {
        var proofs = await service.Get();
        return Ok(proofs);
    }
}

internal class ResponseExamples : ExamplesProvider<GetEnrollmentProofsOut>;
