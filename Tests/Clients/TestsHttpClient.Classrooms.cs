using System.Net.Http.Json;
using Estud.Back.Features.Classrooms.GetClassroom;
using Estud.Back.Features.Classrooms.GetClassrooms;
using Estud.Back.Features.Classrooms.CreateClassroom;
using Estud.Back.Features.Classrooms.UpdateClassroom;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<GetClassroomOut, ErrorOut>> GetClassroom(int classroomId)
    {
        var response = await http.GetAsync($"classrooms/{classroomId}");
        return await response.Resolve<GetClassroomOut>();
    }

    public async Task<OneOf<CreateClassroomOut, ErrorOut>> CreateClassroom(
        int campusId,
        string name = "Sala 05",
        int capacity = 40
    ) {
        var data = new CreateClassroomIn { CampusId = campusId, Name = name, Capacity = capacity };
        var response = await http.PostAsJsonAsync("classrooms", data);
        return await response.Resolve<CreateClassroomOut>();
    }

    public async Task<OneOf<UpdateClassroomOut, ErrorOut>> UpdateClassroom(
        int classroomId,
        string name = "Sala 10",
        int capacity = 50
    ) {
        var data = new UpdateClassroomIn { Name = name, Capacity = capacity };
        var response = await http.PutAsJsonAsync($"classrooms/{classroomId}", data);
        return await response.Resolve<UpdateClassroomOut>();
    }

    public async Task<OneOf<List<GetClassroomsOut>, ErrorOut>> GetClassrooms()
    {
        var response = await http.GetAsync("classrooms");
        return await response.Resolve<List<GetClassroomsOut>>();
    }
}
