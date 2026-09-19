using System.Net.Http.Json;
using Estud.Back.Features.Teachers.GetTeacher;
using Estud.Back.Features.Teachers.GetTeachers;
using Estud.Back.Features.Teachers.CreateTeacher;
using Estud.Back.Features.Teachers.UpdateTeacher;
using Estud.Back.Features.Teachers.GetTeacherHome;
using Estud.Back.Features.Teachers.GetTeacherClass;
using Estud.Back.Features.Teachers.AddActivityNote;
using Estud.Back.Features.Teachers.GetTeacherAgenda;
using Estud.Back.Features.Teachers.UpdateLessonPlan;
using Estud.Back.Features.Teachers.GetTeacherDetails;
using Estud.Back.Features.Teachers.CreateClassActivity;
using Estud.Back.Features.Teachers.UpdateClassActivity;
using Estud.Back.Features.Teachers.AssignCampiToTeacher;
using Estud.Back.Features.Teachers.CreateLessonPlanFile;
using Estud.Back.Features.Teachers.GetTeacherClassLesson;
using Estud.Back.Features.Teachers.CreateLessonAttendance;
using Estud.Back.Features.Teachers.GetTeacherClassLessons;
using Estud.Back.Features.Teachers.GetTeacherClassActivity;
using Estud.Back.Features.Teachers.GetTeacherClassStudents;
using Estud.Back.Features.Teachers.CreateClassActivityFile;
using Estud.Back.Features.Teachers.GetTeacherCurrentClasses;
using Estud.Back.Features.Teachers.GetTeacherPotentialCampi;
using Estud.Back.Features.Teachers.GetTeacherClassActivities;
using Estud.Back.Features.Teachers.AssignDisciplinesToTeacher;
using Estud.Back.Features.Teachers.GetTeacherPotentialDisciplines;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateTeacherOut, ErrorOut>> CreateTeacher(
        string name,
        string email
    ) {
        var data = new CreateTeacherIn { Name = name, Email = email };
        var response = await http.PostAsJsonAsync("/teachers", data);
        return await response.Resolve<CreateTeacherOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AssignCampiToTeacher(
        int teacherId,
        List<int> campi
    ) {
        var data = new AssignCampiToTeacherIn { Campi = campi };
        var response = await http.PutAsJsonAsync($"/teachers/{teacherId}/assign-campi", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AssignDisciplinesToTeacher(
        int teacherId,
        List<int> disciplines
    ) {
        var data = new AssignDisciplinesToTeacherIn { Disciplines = disciplines };
        var response = await http.PutAsJsonAsync($"/teachers/{teacherId}/assign-disciplines", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<GetTeachersOut, ErrorOut>> GetTeachers(
        string? filter = null,
        int? page = null,
        int? pageSize = null
    ) {
        var data = new GetTeachersIn
        {
            Filter = filter,
            Page = page ?? 1,
            PageSize = pageSize ?? 10,
        };

        var response = await http.GetAsync("/teachers".AddQueryString(data));
        return await response.Resolve<GetTeachersOut>();
    }

    public async Task<OneOf<GetTeacherOut, ErrorOut>> GetTeacher(int teacherId)
    {
        var response = await http.GetAsync($"/teachers/{teacherId}");
        return await response.Resolve<GetTeacherOut>();
    }

    public async Task<OneOf<GetTeacherDetailsOut, ErrorOut>> GetTeacherDetails(int teacherId)
    {
        var response = await http.GetAsync($"/teachers/{teacherId}/details");
        return await response.Resolve<GetTeacherDetailsOut>();
    }

    public async Task<OneOf<GetTeacherPotentialCampiOut, ErrorOut>> GetTeacherPotentialCampi(int teacherId, string? name = null)
    {
        var url = $"/teachers/{teacherId}/potential-campi";
        if (name != null) url += $"?name={name}";
        var response = await http.GetAsync(url);
        return await response.Resolve<GetTeacherPotentialCampiOut>();
    }

    public async Task<OneOf<GetTeacherPotentialDisciplinesOut, ErrorOut>> GetTeacherPotentialDisciplines(int teacherId, string? name = null)
    {
        var url = $"/teachers/{teacherId}/potential-disciplines";
        if (name != null) url += $"?name={name}";
        var response = await http.GetAsync(url);
        return await response.Resolve<GetTeacherPotentialDisciplinesOut>();
    }

    public async Task<OneOf<GetTeacherHomeOut, ErrorOut>> GetTeacherHome()
    {
        var response = await http.GetAsync("/teachers/home");
        return await response.Resolve<GetTeacherHomeOut>();
    }

    public async Task<OneOf<GetTeacherCurrentClassesOut, ErrorOut>> GetTeacherCurrentClasses()
    {
        var response = await http.GetAsync("/teachers/current-classes");
        return await response.Resolve<GetTeacherCurrentClassesOut>();
    }

    public async Task<OneOf<GetTeacherClassOut, ErrorOut>> GetTeacherClass(int classId)
    {
        var response = await http.GetAsync($"/teachers/classes/{classId}");
        return await response.Resolve<GetTeacherClassOut>();
    }

    public async Task<OneOf<GetTeacherAgendaOut, ErrorOut>> GetTeacherAgenda()
    {
        var response = await http.GetAsync("/teachers/agenda");
        return await response.Resolve<GetTeacherAgendaOut>();
    }

    public async Task<OneOf<CreateClassActivityOut, ErrorOut>> CreateClassActivity(
        int classId,
        ClassNoteType note = ClassNoteType.N1,
        string title = "Modelagem de Banco de Dados",
        string description = "Modele um banco de dados para um sistema de gerenciamento de biblioteca.",
        ClassActivityType type = ClassActivityType.Work,
        int weight = 40,
        DateOnly? dueDate = null,
        Hour dueHour = Hour.H19_00
    ) {
        var data = new CreateClassActivityIn
        {
            Note = note,
            Title = title,
            Description = description,
            Type = type,
            Weight = weight,
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(7).ToDateOnly(),
            DueHour = dueHour,
        };
        var response = await http.PostAsJsonAsync($"/teachers/classes/{classId}/activities", data);
        return await response.Resolve<CreateClassActivityOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> UpdateClassActivity(
        int classId,
        int activityId,
        ClassNoteType note = ClassNoteType.N1,
        string title = "Modelagem de Banco de Dados",
        string description = "Modele um banco de dados para um sistema de gerenciamento de biblioteca.",
        ClassActivityType type = ClassActivityType.Work,
        int weight = 40,
        DateOnly? dueDate = null,
        Hour dueHour = Hour.H19_00,
        bool notifyStudents = false
    ) {
        var data = new UpdateClassActivityIn
        {
            Note = note,
            Title = title,
            Description = description,
            Type = type,
            Weight = weight,
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(7).ToDateOnly(),
            DueHour = dueHour,
            NotifyStudents = notifyStudents,
        };
        var response = await http.PutAsJsonAsync($"/teachers/classes/{classId}/activities/{activityId}", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<CreateClassActivityFileOut, ErrorOut>> CreateClassActivityFile(
        int classId,
        string contentType = "application/pdf",
        long sizeInBytes = 1_850_000
    ) {
        var data = new CreateClassActivityFileIn { ContentType = contentType, SizeInBytes = sizeInBytes };
        var response = await http.PostAsJsonAsync($"/teachers/classes/{classId}/activities/files", data);
        return await response.Resolve<CreateClassActivityFileOut>();
    }

    public async Task<OneOf<GetTeacherClassActivitiesOut, ErrorOut>> GetTeacherClassActivities(int classId)
    {
        var response = await http.GetAsync($"/teachers/classes/{classId}/activities");
        return await response.Resolve<GetTeacherClassActivitiesOut>();
    }

    public async Task<OneOf<GetTeacherClassActivityOut, ErrorOut>> GetTeacherClassActivity(int classId, int activityId)
    {
        var response = await http.GetAsync($"/teachers/classes/{classId}/activities/{activityId}");
        return await response.Resolve<GetTeacherClassActivityOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> AddActivityNote(
        int activityId,
        int workId,
        decimal note = 8.5m
    ) {
        var data = new AddActivityNoteIn { Note = note };
        var response = await http.PutAsJsonAsync($"/teachers/activities/{activityId}/works/{workId}/note", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<GetTeacherClassStudentsOut, ErrorOut>> GetTeacherClassStudents(int classId)
    {
        var response = await http.GetAsync($"/teachers/classes/{classId}/students");
        return await response.Resolve<GetTeacherClassStudentsOut>();
    }

    public async Task<OneOf<GetTeacherClassLessonOut, ErrorOut>> GetTeacherClassLesson(int classId, int lessonId)
    {
        var response = await http.GetAsync($"/teachers/classes/{classId}/lessons/{lessonId}");
        return await response.Resolve<GetTeacherClassLessonOut>();
    }

    public async Task<OneOf<GetTeacherClassLessonsOut, ErrorOut>> GetTeacherClassLessons(int classId)
    {
        var response = await http.GetAsync($"/teachers/classes/{classId}/lessons");
        return await response.Resolve<GetTeacherClassLessonsOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> CreateLessonAttendance(
        int lessonId,
        List<int> presentStudents
    ) {
        var data = new CreateLessonAttendanceIn { PresentStudents = presentStudents };
        var response = await http.PutAsJsonAsync($"/teachers/lessons/{lessonId}/attendance", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> UpdateLessonPlan(
        int lessonId,
        string? plannedContent
    ) {
        var data = new UpdateLessonPlanIn { PlannedContent = plannedContent };
        var response = await http.PutAsJsonAsync($"/teachers/lessons/{lessonId}/plan", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<CreateLessonPlanFileOut, ErrorOut>> CreateLessonPlanFile(
        int lessonId,
        string contentType = "image/png",
        long sizeInBytes = 245_000
    ) {
        var data = new CreateLessonPlanFileIn { ContentType = contentType, SizeInBytes = sizeInBytes };
        var response = await http.PostAsJsonAsync($"/teachers/lessons/{lessonId}/plan/files", data);
        return await response.Resolve<CreateLessonPlanFileOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> UpdateTeacher(
        int teacherId,
        string name = "Richard Feynman",
        string email = "feynman@estud.com"
    ) {
        var data = new UpdateTeacherIn { Name = name, Email = email };
        var response = await http.PutAsJsonAsync($"/teachers/{teacherId}", data);
        return await response.Resolve<SuccessOut>();
    }
}
