namespace Estud.Back.Errors;

public class ClassNotFound : EstudError
{
    public static readonly ClassNotFound I = new();
    public override string Code { get; set; } = nameof(ClassNotFound);
    public override string Message { get; set; } = "Turma não encontrada.";
}

public class ClassMustBeOnPreEnrollment : EstudError
{
    public static readonly ClassMustBeOnPreEnrollment I = new();
    public override string Code { get; set; } = nameof(ClassMustBeOnPreEnrollment);
    public override string Message { get; set; } = "A turma deve estar em pré-matrícula.";
}

public class ClassMustBeOnEnrollment : EstudError
{
    public static readonly ClassMustBeOnEnrollment I = new();
    public override string Code { get; set; } = nameof(ClassMustBeOnEnrollment);
    public override string Message { get; set; } = "A turma deve estar em matrícula.";
}

public class ClassMustBeStarted : EstudError
{
    public static readonly ClassMustBeStarted I = new();
    public override string Code { get; set; } = nameof(ClassMustBeStarted);
    public override string Message { get; set; } = "A turma deve estar iniciada.";
}

public class ClassAlreadyStarted : EstudError
{
    public static readonly ClassAlreadyStarted I = new();
    public override string Code { get; set; } = nameof(ClassAlreadyStarted);
    public override string Message { get; set; } = "A turma já foi iniciada.";
}

public class ClassAlreadyFinalized : EstudError
{
    public static readonly ClassAlreadyFinalized I = new();
    public override string Code { get; set; } = nameof(ClassAlreadyFinalized);
    public override string Message { get; set; } = "A turma já foi finalizada.";
}

public class ClassWithoutTeachers : EstudError
{
    public static readonly ClassWithoutTeachers I = new();
    public override string Code { get; set; } = nameof(ClassWithoutTeachers);
    public override string Message { get; set; } = "A turma não possui professores atribuídos.";
}

public class ClassWithoutSchedules : EstudError
{
    public static readonly ClassWithoutSchedules I = new();
    public override string Code { get; set; } = nameof(ClassWithoutSchedules);
    public override string Message { get; set; } = "A turma não possui horários definidos.";
}

public class TeacherScheduleConflict : EstudError
{
    public static readonly TeacherScheduleConflict I = new();
    public override string Code { get; set; } = nameof(TeacherScheduleConflict);
    public override string Message { get; set; } = "Conflito de horário com outra turma do professor.";
}

public class InvalidScheduleTeacher : EstudError
{
    public static readonly InvalidScheduleTeacher I = new();
    public override string Code { get; set; } = nameof(InvalidScheduleTeacher);
    public override string Message { get; set; } = "Professor do horário não pertence à turma.";
}

public class InvalidClassVacancies : EstudError
{
    public static readonly InvalidClassVacancies I = new();
    public override string Code { get; set; } = nameof(InvalidClassVacancies);
    public override string Message { get; set; } = "Número de vagas inválido.";
}

public class NoVacanciesInClass : EstudError
{
    public static readonly NoVacanciesInClass I = new();
    public override string Code { get; set; } = nameof(NoVacanciesInClass);
    public override string Message { get; set; } = "A turma não possui vagas disponíveis.";
}

public class InvalidDay : EstudError
{
    public static readonly InvalidDay I = new();
    public override string Code { get; set; } = nameof(InvalidDay);
    public override string Message { get; set; } = "Dia inválido.";
}

public class InvalidHour : EstudError
{
    public static readonly InvalidHour I = new();
    public override string Code { get; set; } = nameof(InvalidHour);
    public override string Message { get; set; } = "Hora inválida.";
}

public class InvalidSchedule : EstudError
{
    public static readonly InvalidSchedule I = new();
    public override string Code { get; set; } = nameof(InvalidSchedule);
    public override string Message { get; set; } = "Horário inválido.";
}

public class ConflictingSchedules : EstudError
{
    public static readonly ConflictingSchedules I = new();
    public override string Code { get; set; } = nameof(ConflictingSchedules);
    public override string Message { get; set; } = "Horários conflitantes.";
}

public class InvalidClassActivityWeight : EstudError
{
    public static readonly InvalidClassActivityWeight I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityWeight);
    public override string Message { get; set; } = "Peso da atividade inválido.";
}

public class NoteTypeNotUsedByInstitution : EstudError
{
    public static readonly NoteTypeNotUsedByInstitution I = new();
    public override string Code { get; set; } = nameof(NoteTypeNotUsedByInstitution);
    public override string Message { get; set; } = "Tipo de nota não utilizado pela instituição.";
}

public class InvalidStudentClassNote : EstudError
{
    public static readonly InvalidStudentClassNote I = new();
    public override string Code { get; set; } = nameof(InvalidStudentClassNote);
    public override string Message { get; set; } = "Nota da atividade inválida.";
}

public class ClassActivityNotFound : EstudError
{
    public static readonly ClassActivityNotFound I = new();
    public override string Code { get; set; } = nameof(ClassActivityNotFound);
    public override string Message { get; set; } = "Atividade não encontrada.";
}

public class ClassActivityWorkNotFound : EstudError
{
    public static readonly ClassActivityWorkNotFound I = new();
    public override string Code { get; set; } = nameof(ClassActivityWorkNotFound);
    public override string Message { get; set; } = "Entrega da atividade não encontrada.";
}

public class InvalidClassActivityWorkContent : EstudError
{
    public static readonly InvalidClassActivityWorkContent I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityWorkContent);
    public override string Message { get; set; } = "Conteúdo da entrega inválido.";
}

public class ClassActivityDoesNotAcceptWorks : EstudError
{
    public static readonly ClassActivityDoesNotAcceptWorks I = new();
    public override string Code { get; set; } = nameof(ClassActivityDoesNotAcceptWorks);
    public override string Message { get; set; } = "Esta atividade não aceita entregas.";
}

public class ClassLessonNotFound : EstudError
{
    public static readonly ClassLessonNotFound I = new();
    public override string Code { get; set; } = nameof(ClassLessonNotFound);
    public override string Message { get; set; } = "Aula não encontrada.";
}

public class ClassLessonNotStarted : EstudError
{
    public static readonly ClassLessonNotStarted I = new();
    public override string Code { get; set; } = nameof(ClassLessonNotStarted);
    public override string Message { get; set; } = "Não é possível fazer a chamada de uma aula que ainda não aconteceu.";
}

public class InvalidClassLessonPlan : EstudError
{
    public static readonly InvalidClassLessonPlan I = new();
    public override string Code { get; set; } = nameof(InvalidClassLessonPlan);
    public override string Message { get; set; } = "Planejamento da aula inválido.";
}

public class InvalidClassLessonSearch : EstudError
{
    public static readonly InvalidClassLessonSearch I = new();
    public override string Code { get; set; } = nameof(InvalidClassLessonSearch);
    public override string Message { get; set; } = "O termo de busca deve ter entre 3 e 100 caracteres.";
}

public class InvalidLessonPlanFileContentType : EstudError
{
    public static readonly InvalidLessonPlanFileContentType I = new();
    public override string Code { get; set; } = nameof(InvalidLessonPlanFileContentType);
    public override string Message { get; set; } = "Formato de arquivo inválido. Envie PNG, JPEG, WebP ou PDF.";
}

public class InvalidLessonPlanFileSize : EstudError
{
    public static readonly InvalidLessonPlanFileSize I = new();
    public override string Code { get; set; } = nameof(InvalidLessonPlanFileSize);
    public override string Message { get; set; } = "A imagem deve ter no máximo 5 MB e o PDF, 10 MB.";
}

public class InvalidClassActivityFileContentType : EstudError
{
    public static readonly InvalidClassActivityFileContentType I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityFileContentType);
    public override string Message { get; set; } = "Formato de arquivo inválido. Envie PNG, JPEG, WebP ou PDF.";
}

public class InvalidClassActivityFileSize : EstudError
{
    public static readonly InvalidClassActivityFileSize I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityFileSize);
    public override string Message { get; set; } = "A imagem deve ter no máximo 5 MB e o PDF, 10 MB.";
}

public class InvalidClassActivityWorkFileContentType : EstudError
{
    public static readonly InvalidClassActivityWorkFileContentType I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityWorkFileContentType);
    public override string Message { get; set; } = "Formato de arquivo inválido. Envie PNG, JPEG, WebP ou PDF.";
}

public class InvalidClassActivityWorkFileSize : EstudError
{
    public static readonly InvalidClassActivityWorkFileSize I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityWorkFileSize);
    public override string Message { get; set; } = "A imagem deve ter no máximo 5 MB e o PDF, 10 MB.";
}

public class InvalidStudentsList : EstudError
{
    public static readonly InvalidStudentsList I = new();
    public override string Code { get; set; } = nameof(InvalidStudentsList);
    public override string Message { get; set; } = "Lista de alunos inválida.";
}

public class ClassActivityWorkAlreadyFinalized : EstudError
{
    public static readonly ClassActivityWorkAlreadyFinalized I = new();
    public override string Code { get; set; } = nameof(ClassActivityWorkAlreadyFinalized);
    public override string Message { get; set; } = "Entrega já finalizada pelo professor.";
}

public class InvalidClassActivityWorkEntry : EstudError
{
    public static readonly InvalidClassActivityWorkEntry I = new();
    public override string Code { get; set; } = nameof(InvalidClassActivityWorkEntry);
    public override string Message { get; set; } = "Informe um comentário, uma nota ou um status.";
}
