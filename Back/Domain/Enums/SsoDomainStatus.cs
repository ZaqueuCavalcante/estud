namespace Estud.Back.Domain.Enums;

public enum SsoDomainStatus
{
    [Description("Pendente")]
    Pending = 0,

    [Description("Verificado")]
    Verified = 1,

    [Description("Falha na verificação")]
    Failed = 2,

    [Description("Expirado")]
    Expired = 3,
}
