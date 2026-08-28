namespace Estud.Back.Features.Disciplines.GetDisciplines;

public static class GetDisciplinesMapper
{
    extension(DisciplineRow row)
    {
        public GetDisciplinesItemOut ToGetDisciplinesItemOut()
        {
            return new()
            {
                Id = row.Id,
                Name = row.Name,
                Code = row.Code,
                HasCourses = row.HasCourses,
                HasTeachers = row.HasTeachers,
            };
        }
    }
}
