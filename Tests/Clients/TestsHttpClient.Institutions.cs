using System.Net.Http.Json;
using Estud.Back.Features.Institutions.GetInstitutionConfig;
using Estud.Back.Features.Institutions.SetupInstitutionConfig;
using Estud.Back.Features.Institutions.GetInstitutionNoteTypes;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<GetInstitutionConfigOut, ErrorOut>> GetInstitutionConfig()
    {
        var response = await http.GetAsync("/institutions/config");
        return await response.Resolve<GetInstitutionConfigOut>();
    }

    public async Task<OneOf<GetInstitutionNoteTypesOut, ErrorOut>> GetInstitutionNoteTypes()
    {
        var response = await http.GetAsync("/institutions/note-types");
        return await response.Resolve<GetInstitutionNoteTypesOut>();
    }

    public async Task<OneOf<SetupInstitutionConfigOut, ErrorOut>> SetupInstitutionConfig(
        decimal noteLimit = 7.00M,
        decimal frequencyLimit = 70.00M,
        ClassGradeRule gradeRule = ClassGradeRule.BestTwoOfThree
    ) {
        var data = new SetupInstitutionConfigIn
        {
            NoteLimit = noteLimit,
            FrequencyLimit = frequencyLimit,
            GradeRule = gradeRule,
        };
        var response = await http.PostAsJsonAsync("/institutions/config", data);
        return await response.Resolve<SetupInstitutionConfigOut>();
    }
}
