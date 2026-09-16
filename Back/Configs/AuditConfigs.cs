using Audit.Core;
using Estud.Back.Audit;
using Audit.EntityFramework;
using Estud.Back.Domain.Identity;
using Estud.Back.Domain.Webhooks;
using Estud.Back.Domain.Institutions;
using AuditConfig = Audit.Core.Configuration;

namespace Estud.Back.Configs;

public static class AuditConfigs
{
    public static void AddAuditConfigs(this WebApplicationBuilder _)
    {
        AuditConfig.Setup().UseEntityFramework(_ => _
            .AuditTypeExplicitMapper(_ => _
                .Map<EstudRole, AuditTrail>()
                .Map<EstudUser, AuditTrail>()
                .Map<MagicLink, AuditTrail>()
                .Map<SsoConfiguration, AuditTrail>()
                .Map<SsoAllowedDomain, AuditTrail>()
                .Map<InstitutionConfig, AuditTrail>()
                .Map<WebhookSubscription, AuditTrail>()
                .AuditEntityAction<AuditTrail>((evt, entry, trail) =>
                {
                    if (evt.Environment.Exception != null) return false;
                    return trail.Fill(evt, entry);
                }))
            .IgnoreMatchedProperties(true));

        AuditConfig.AddCustomAction(ActionType.OnScopeCreated, scope =>
        {
            var dbContext = scope.GetEntityFrameworkEvent().GetDbContext() as EstudDbContext;

            scope.SetUserId(dbContext.RequestUser.Id);
            scope.SetInstitutionId(dbContext.RequestUser.InstitutionId);
            scope.SetActivityId(dbContext.ActivityId);
            scope.SetOperation(dbContext.Operation);
        });
    }
}
