namespace Estud.Back.Errors;

public class ParentNotFound : EstudError
{
    public static readonly ParentNotFound I = new();
    public override string Code { get; set; } = nameof(ParentNotFound);
    public override string Message { get; set; } = "Responsável não encontrado.";
}

public class InvalidParentStudentsList : EstudError
{
    public static readonly InvalidParentStudentsList I = new();
    public override string Code { get; set; } = nameof(InvalidParentStudentsList);
    public override string Message { get; set; } = "Lista de alunos vinculados inválida.";
}

public class InvalidParentRelationship : EstudError
{
    public static readonly InvalidParentRelationship I = new();
    public override string Code { get; set; } = nameof(InvalidParentRelationship);
    public override string Message { get; set; } = "Parentesco inválido.";
}

public class ParentStudentLinkNotFound : EstudError
{
    public static readonly ParentStudentLinkNotFound I = new();
    public override string Code { get; set; } = nameof(ParentStudentLinkNotFound);
    public override string Message { get; set; } = "Vínculo entre responsável e aluno não encontrado.";
}

public class ParentStudentLinkAlreadyRevoked : EstudError
{
    public static readonly ParentStudentLinkAlreadyRevoked I = new();
    public override string Code { get; set; } = nameof(ParentStudentLinkAlreadyRevoked);
    public override string Message { get; set; } = "Vínculo já revogado.";
}

public class StudentMustBeAdult : EstudError
{
    public static readonly StudentMustBeAdult I = new();
    public override string Code { get; set; } = nameof(StudentMustBeAdult);
    public override string Message { get; set; } = "Apenas alunos maiores de 18 anos podem revogar o acesso do responsável.";
}
