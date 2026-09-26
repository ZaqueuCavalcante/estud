using System.Collections.Concurrent;

namespace Estud.Fakes.Oidc;

public record OidcFakeUser(
    string? Email,
    string Subject,
    string ClientId,
    string? Nonce,
    string? CodeChallenge,
    bool EmailVerified = true,
    string? Name = null,
    string? UserInfoSubject = null
);

/// <summary>
/// Estado por fluxo de login, para que testes em paralelo não disputem um usuário global.
/// O e-mail entra pelo login_hint no authorize e é resolvido depois pelo code e pelo access token.
/// </summary>
public static class OidcFakeUsers
{
    public static readonly ConcurrentDictionary<string, OidcFakeUser> ByCode = new();
    public static readonly ConcurrentDictionary<string, OidcFakeUser> ByAccessToken = new();
}
