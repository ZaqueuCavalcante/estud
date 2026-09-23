using Estud.Back.Domain.Institutions;

namespace Estud.Back.Features.Institutions.GetInstitutionConfig;

public static class GetInstitutionConfigMapper
{
    extension(Institution institution)
    {
        public GetInstitutionConfigOut ToGetInstitutionConfigOut()
        {
            return new()
            {
                Id = institution.Config.Id,
                Name = institution.Name,
                GradeRule = institution.Config.GradeRule,
                NoteLimit = institution.Config.NoteLimit,
                FrequencyLimit = institution.Config.FrequencyLimit,
            };
        }
    }
}
