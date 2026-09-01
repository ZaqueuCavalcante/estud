using System.Net.Http.Json;
using Estud.Back.Features.Students.GetStudent;
using Estud.Back.Features.Students.GetStudents;
using Estud.Back.Features.Students.CreateStudent;
using Estud.Back.Features.Students.GetStudentClass;
using Estud.Back.Features.Students.GetStudentAgenda;
using Estud.Back.Features.Students.GetStudentDetails;
using Estud.Back.Features.Students.GetEnrollmentProofs;
using Estud.Back.Features.Students.AssignStudentToClass;
using Estud.Back.Features.Students.CreateClassActivityWork;
using Estud.Back.Features.Students.GetStudentClassActivity;
using Estud.Back.Features.Students.GetStudentCourseDetails;
using Estud.Back.Features.Students.ValidateEnrollmentProof;
using Estud.Back.Features.Students.GetStudentClassActivities;
using Estud.Back.Features.Students.GetStudentAttendanceCalendar;
using Estud.Back.Features.Students.EnrollStudentInCourseOffering;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateStudentOut, ErrorOut>> CreateStudent(
        string name,
        string email,
        string? phoneNumber = null,
        DateOnly? birthdate = null
    ) {
        var data = new CreateStudentIn { Name = name, Email = email, PhoneNumber = phoneNumber, Birthdate = birthdate };
        var response = await http.PostAsJsonAsync("/students", data);
        return await response.Resolve<CreateStudentOut>();
    }

    public async Task<OneOf<GetStudentsOut, ErrorOut>> GetStudents(
        string? filter = null,
        int? page = null,
        int? pageSize = null
    ) {
        var data = new GetStudentsIn
        {
            Filter = filter,
            Page = page ?? 1,
            PageSize = pageSize ?? 10,
        };

        var response = await http.GetAsync("/students".AddQueryString(data));
        return await response.Resolve<GetStudentsOut>();
    }

    public async Task<OneOf<GetStudentOut, ErrorOut>> GetStudent(int studentId)
    {
        var response = await http.GetAsync($"/students/{studentId}");
        return await response.Resolve<GetStudentOut>();
    }

    public async Task<OneOf<GetStudentDetailsOut, ErrorOut>> GetStudentDetails(int studentId)
    {
        var response = await http.GetAsync($"/students/{studentId}/details");
        return await response.Resolve<GetStudentDetailsOut>();
    }

    public async Task<OneOf<GetStudentCourseDetailsOut, ErrorOut>> GetStudentCourseDetails()
    {
        var response = await http.GetAsync("/students/course");
        return await response.Resolve<GetStudentCourseDetailsOut>();
    }

    public async Task<OneOf<GetStudentClassOut, ErrorOut>> GetStudentClass(int classId)
    {
        var response = await http.GetAsync($"/students/classes/{classId}");
        return await response.Resolve<GetStudentClassOut>();
    }

    public async Task<OneOf<GetStudentAttendanceCalendarOut, ErrorOut>> GetStudentAttendanceCalendar(int? year = null)
    {
        var data = new GetStudentAttendanceCalendarIn { Year = year };
        var response = await http.GetAsync("/students/attendances/calendar".AddQueryString(data));
        return await response.Resolve<GetStudentAttendanceCalendarOut>();
    }

    public async Task<OneOf<GetStudentClassActivitiesOut, ErrorOut>> GetStudentClassActivities(int classId)
    {
        var response = await http.GetAsync($"/students/classes/{classId}/activities");
        return await response.Resolve<GetStudentClassActivitiesOut>();
    }

    public async Task<OneOf<GetStudentClassActivityOut, ErrorOut>> GetStudentClassActivity(int classId, int activityId)
    {
        var response = await http.GetAsync($"/students/classes/{classId}/activities/{activityId}");
        return await response.Resolve<GetStudentClassActivityOut>();
    }

    public async Task<OneOf<CreateClassActivityWorkOut, ErrorOut>> CreateClassActivityWork(
        int activityId,
        string? link = "https://github.com/ZaqueuCavalcante/estud"
    ) {
        var data = new CreateClassActivityWorkIn { Link = link };
        var response = await http.PostAsJsonAsync($"/students/activities/{activityId}/works", data);
        return await response.Resolve<CreateClassActivityWorkOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AssignStudentToClass(int studentId, int classId)
    {
        var data = new AssignStudentToClassIn { ClassId = classId };
        var response = await http.PostAsJsonAsync($"/students/{studentId}/classes", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<EnrollStudentInCourseOfferingOut, ErrorOut>> EnrollStudentInCourseOffering(int studentId, int courseOfferingId)
    {
        var data = new EnrollStudentInCourseOfferingIn { CourseOfferingId = courseOfferingId };
        var response = await http.PostAsJsonAsync($"/students/{studentId}/course-offerings", data);
        return await response.Resolve<EnrollStudentInCourseOfferingOut>();
    }

    public async Task<OneOf<byte[], ErrorOut>> GenerateEnrollmentProof()
    {
        var response = await http.PostAsync("/students/enrollment-proofs", null);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsByteArrayAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return UnauthorizedErrorOut.I;

        if (response.StatusCode == HttpStatusCode.Forbidden)
            return ForbiddenErrorOut.I;

        return await response.ToError();
    }

    public async Task<OneOf<GetEnrollmentProofsOut, ErrorOut>> GetEnrollmentProofs()
    {
        var response = await http.GetAsync("/students/enrollment-proofs");
        return await response.Resolve<GetEnrollmentProofsOut>();
    }

    public async Task<OneOf<ValidateEnrollmentProofOut, ErrorOut>> ValidateEnrollmentProof(string code)
    {
        var response = await http.PostAsync($"/students/enrollment-proofs/{code}/validate", null);
        return await response.Resolve<ValidateEnrollmentProofOut>();
    }

    public async Task<OneOf<GetStudentAgendaOut, ErrorOut>> GetStudentAgenda()
    {
        var response = await http.GetAsync("/students/agenda");
        return await response.Resolve<GetStudentAgendaOut>();
    }
}
