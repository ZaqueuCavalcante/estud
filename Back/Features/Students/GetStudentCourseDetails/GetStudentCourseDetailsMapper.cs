namespace Estud.Back.Features.Students.GetStudentCourseDetails;

public static class GetStudentCourseDetailsMapper
{
    private static readonly StudentDisciplineStatus[] StatusPriority =
    [
        StudentDisciplineStatus.Aprovada,
        StudentDisciplineStatus.Dispensada,
        StudentDisciplineStatus.Cursando,
        StudentDisciplineStatus.Reprovada,
    ];

    extension(StudentClassStatus status)
    {
        public StudentDisciplineStatus ToStudentDisciplineStatus()
        {
            return status switch
            {
                StudentClassStatus.Aprovado => StudentDisciplineStatus.Aprovada,
                StudentClassStatus.Dispensado => StudentDisciplineStatus.Dispensada,
                StudentClassStatus.ReprovadoPorNota => StudentDisciplineStatus.Reprovada,
                StudentClassStatus.ReprovadoPorFalta => StudentDisciplineStatus.Reprovada,
                StudentClassStatus.Matriculado => StudentDisciplineStatus.Cursando,
                _ => StudentDisciplineStatus.NaoCursada,
            };
        }
    }

    extension(IEnumerable<(int ClassId, StudentClassStatus Status)> classes)
    {
        /// <summary>
        /// Um aluno pode ter cursado a mesma disciplina em várias turmas (ex: reprovou e refez).
        /// A prioridade escolhe o desfecho mais relevante: aprovação/dispensa (conclusão) primeiro,
        /// depois "cursando", depois reprovação, e por fim "não cursada" (sem turma).
        /// Entre turmas com o mesmo desfecho, vale a mais recente.
        /// </summary>
        public (int? ClassId, StudentDisciplineStatus Status) ToBestStudentClass()
        {
            var ranked = classes
                .Select(c => (c.ClassId, Status: c.Status.ToStudentDisciplineStatus()))
                .Where(c => StatusPriority.Contains(c.Status))
                .OrderBy(c => Array.IndexOf(StatusPriority, c.Status))
                .ThenByDescending(c => c.ClassId)
                .ToList();

            if (ranked.Count == 0) return (null, StudentDisciplineStatus.NaoCursada);

            return (ranked[0].ClassId, ranked[0].Status);
        }
    }
}
