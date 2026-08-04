using System.Net.Http.Json;
using Estud.Back.Features.Campi.GetCampi;
using Estud.Back.Features.Campi.CreateCampus;
using Estud.Back.Features.Campi.UpdateCampus;
using Estud.Back.Features.Campi.GetCampusOccupancy;
using Estud.Back.Features.Campi.GetCampusOpeningHours;
using Estud.Back.Features.Campi.UpdateCampusOpeningHours;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateCampusOut, ErrorOut>> CreateCampus(
        string name = "Agreste I",
        BrazilState? state = BrazilState.PE,
        string city = "Caruaru"
    ) {
        var data = new CreateCampusIn { Name = name, State = state, City = city };
        var response = await http.PostAsJsonAsync("/campi", data);
        return await response.Resolve<CreateCampusOut>();
    }

    public async Task<OneOf<UpdateCampusOut, ErrorOut>> UpdateCampus(
        int campusId,
        string name = "Agreste II",
        BrazilState? state = BrazilState.PE,
        string city = "Bonito"
    ) {
        var data = new UpdateCampusIn { Name = name, State = state, City = city };
        var response = await http.PutAsJsonAsync($"/campi/{campusId}", data);
        return await response.Resolve<UpdateCampusOut>();
    }

    public async Task<OneOf<GetCampiOut, ErrorOut>> GetCampi()
    {
        var response = await http.GetAsync("/campi");
        return await response.Resolve<GetCampiOut>();
    }

    public async Task<OneOf<GetCampusOccupancyOut, ErrorOut>> GetCampusOccupancy(int campusId)
    {
        var response = await http.GetAsync($"/campi/{campusId}/occupancy");
        return await response.Resolve<GetCampusOccupancyOut>();
    }

    public async Task<OneOf<GetCampusOpeningHoursOut, ErrorOut>> GetCampusOpeningHours(int campusId)
    {
        var response = await http.GetAsync($"/campi/{campusId}/opening-hours");
        return await response.Resolve<GetCampusOpeningHoursOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> UpdateCampusOpeningHours(
        int campusId,
        List<(Day Day, List<(Hour Start, Hour End)> Windows)> days
    ) {
        var data = new UpdateCampusOpeningHoursIn
        {
            Days = days.ConvertAll(d => new UpdateCampusOpeningHoursDayIn
            {
                Day = d.Day,
                Windows = d.Windows.ConvertAll(w => new UpdateCampusOpeningHoursWindowIn
                {
                    Start = w.Start,
                    End = w.End,
                }),
            }),
        };

        var response = await http.PutAsJsonAsync($"/campi/{campusId}/opening-hours", data);
        return await response.Resolve<SuccessOut>();
    }
}
