using Estud.Back.Domain.Institutions;

namespace Estud.Back.Features.Institutions.SetupInstitutionConfig;

public static class SetupInstitutionConfigMapper
{
    extension(Institution institution)
    {
        public SetupInstitutionConfigOut ToSetupInstitutionConfigOut()
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
