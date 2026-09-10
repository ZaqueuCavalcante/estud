using System.Collections.Concurrent;

namespace Estud.Mocks.Oidc;

public record OidcMockUser(
    string? Email,
    string Subject,
    string ClientId,
    string? Nonce,
    bool EmailVerified = true,
    string? Name = null,
    string? UserInfoSubject = null
);

/// <summary>
/// Estado por fluxo de login, para que testes em paralelo não disputem um usuário global.
/// O e-mail entra pelo login_hint no authorize e é resolvido depois pelo code e pelo access token.
/// </summary>
public static class OidcMockUsers
{
    public static readonly ConcurrentDictionary<string, OidcMockUser> ByCode = new();
    public static readonly ConcurrentDictionary<string, OidcMockUser> ByAccessToken = new();
}
