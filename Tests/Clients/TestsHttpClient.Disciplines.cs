using System.Net.Http.Json;
using Estud.Back.Features.Disciplines.GetDisciplines;
using Estud.Back.Features.Disciplines.CreateDiscipline;
using Estud.Back.Features.Disciplines.UpdateDiscipline;
using Estud.Back.Features.Disciplines.GetDisciplineDetails;
using Estud.Back.Features.Disciplines.GetDisciplineTeachers;
using Estud.Back.Features.Disciplines.AssignCoursesToDiscipline;
using Estud.Back.Features.Disciplines.AssignTeachersToDiscipline;
using Estud.Back.Features.Disciplines.GetDisciplinePotentialCourses;
using Estud.Back.Features.Disciplines.GetDisciplinePotentialTeachers;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateDisciplineOut, ErrorOut>> CreateDiscipline(
        string name = "Geometria"
    ) {
        var data = new CreateDisciplineIn { Name = name };
        var response = await http.PostAsJsonAsync("/disciplines", data);
        return await response.Resolve<CreateDisciplineOut>();
    }

    public async Task<OneOf<GetDisciplineTeachersOut, ErrorOut>> GetDisciplineTeachers(int disciplineId)
    {
        var response = await http.GetAsync($"/disciplines/{disciplineId}/teachers");
        return await response.Resolve<GetDisciplineTeachersOut>();
    }

    public async Task<OneOf<UpdateDisciplineOut, ErrorOut>> UpdateDiscipline(
        int disciplineId,
        string name = "Física II"
    ) {
        var data = new UpdateDisciplineIn { Name = name };
        var response = await http.PutAsJsonAsync($"/disciplines/{disciplineId}", data);
        return await response.Resolve<UpdateDisciplineOut>();
    }

    public async Task<OneOf<GetDisciplineDetailsOut, ErrorOut>> GetDisciplineDetails(int disciplineId)
    {
        var response = await http.GetAsync($"/disciplines/{disciplineId}/details");
        return await response.Resolve<GetDisciplineDetailsOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AssignCoursesToDiscipline(
        int disciplineId,
        List<int> courses
    ) {
        var data = new AssignCoursesToDisciplineIn { Courses = courses };
        var response = await http.PutAsJsonAsync($"/disciplines/{disciplineId}/assign-courses", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<GetDisciplinesOut, ErrorOut>> GetDisciplines(
        string? filter = null,
        bool? hasCourses = null,
        bool? hasTeachers = null,
        int? page = null,
        int? pageSize = null
    ) {
        var data = new GetDisciplinesIn
        {
            Filter = filter,
            HasCourses = hasCourses,
            HasTeachers = hasTeachers,
            Page = page ?? 1,
            PageSize = pageSize ?? 10,
        };

        var response = await http.GetAsync("/disciplines".AddQueryString(data));
        return await response.Resolve<GetDisciplinesOut>();
    }

    public async Task<OneOf<GetDisciplinePotentialCoursesOut, ErrorOut>> GetDisciplinePotentialCourses(
        int disciplineId,
        string? name = null
    ) {
        var url = $"/disciplines/{disciplineId}/potential-courses?name={name}";
        var response = await http.GetAsync(url);
        return await response.Resolve<GetDisciplinePotentialCoursesOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AssignTeachersToDiscipline(
        int disciplineId,
        List<int> teachers
    ) {
        var data = new AssignTeachersToDisciplineIn { Teachers = teachers };
        var response = await http.PutAsJsonAsync($"/disciplines/{disciplineId}/assign-teachers", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<GetDisciplinePotentialTeachersOut, ErrorOut>> GetDisciplinePotentialTeachers(
        int disciplineId,
        string? name = null
    ) {
        var url = $"/disciplines/{disciplineId}/potential-teachers?name={name}";
        var response = await http.GetAsync(url);
        return await response.Resolve<GetDisciplinePotentialTeachersOut>();
    }
}
