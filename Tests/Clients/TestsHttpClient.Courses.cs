using System.Net.Http.Json;
using Estud.Back.Features.Courses.GetCourses;
using Estud.Back.Features.Courses.CreateCourse;
using Estud.Back.Features.Courses.UpdateCourse;
using Estud.Back.Features.Courses.GetCourseDetails;
using Estud.Back.Features.Courses.GetCourseDisciplines;
using Estud.Back.Features.Courses.AssignDisciplinesToCourse;
using Estud.Back.Features.Courses.GetCoursePotentialDisciplines;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateCourseOut, ErrorOut>> CreateCourse(
        string name = "Análise e Desenvolvimento de Sistemas",
        CourseType? type = CourseType.Tecnologo
    ) {
        var data = new CreateCourseIn { Name = name, Type = type };
        var response = await http.PostAsJsonAsync("/courses", data);
        return await response.Resolve<CreateCourseOut>();
    }

    public async Task<OneOf<UpdateCourseOut, ErrorOut>> UpdateCourse(
        int courseId,
        string name = "Direito",
        CourseType? type = CourseType.Bacharelado
    ) {
        var data = new UpdateCourseIn { Name = name, Type = type };
        var response = await http.PutAsJsonAsync($"/courses/{courseId}", data);
        return await response.Resolve<UpdateCourseOut>();
    }

    public async Task<OneOf<GetCourseDetailsOut, ErrorOut>> GetCourseDetails(int courseId)
    {
        var response = await http.GetAsync($"/courses/{courseId}/details");
        return await response.Resolve<GetCourseDetailsOut>();
    }

    public async Task<OneOf<GetCoursesOut, ErrorOut>> GetCourses(
        string? filter = null,
        CourseType? type = null,
        bool? hasCurriculums = null,
        int? page = null,
        int? pageSize = null
    ) {
        var data = new GetCoursesIn
        {
            Filter = filter,
            Type = type,
            HasCurriculums = hasCurriculums,
            Page = page ?? 1,
            PageSize = pageSize ?? 10,
        };

        var response = await http.GetAsync("/courses".AddQueryString(data));
        return await response.Resolve<GetCoursesOut>();
    }

    public async Task<OneOf<GetCourseDisciplinesOut, ErrorOut>> GetCourseDisciplines(int courseId)
    {
        var response = await http.GetAsync($"/courses/{courseId}/disciplines");
        return await response.Resolve<GetCourseDisciplinesOut>();
    }

    public async Task<OneOf<GetCoursePotentialDisciplinesOut, ErrorOut>> GetCoursePotentialDisciplines(
        int courseId,
        string? name = null
    ) {
        var url = $"/courses/{courseId}/potential-disciplines" + (name.IsEmpty() ? "" : $"?name={name}");
        var response = await http.GetAsync(url);
        return await response.Resolve<GetCoursePotentialDisciplinesOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AssignDisciplinesToCourse(
        int courseId,
        List<int> disciplines
    ) {
        var data = new AssignDisciplinesToCourseIn { Disciplines = disciplines };
        var response = await http.PutAsJsonAsync($"/courses/{courseId}/assign-disciplines", data);
        return await response.Resolve<SuccessOut>();
    }
}
