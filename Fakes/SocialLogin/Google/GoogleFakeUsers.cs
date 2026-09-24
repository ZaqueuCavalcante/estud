using System.Collections.Concurrent;

namespace Estud.Fakes.SocialLogin.Google;

public record GoogleFakeUser(
    string? Email,
    string Subject,
    bool EmailVerified = true,
    string? Name = null,
    string? GivenName = null,
    string? FamilyName = null
);

/// <summary>
/// Estado por fluxo de login, para que testes em paralelo não disputem um usuário global.
/// O e-mail entra pelo login_hint no authorize e é resolvido depois pelo code e pelo access token.
/// </summary>
public static class GoogleFakeUsers
{
    public static readonly ConcurrentDictionary<string, GoogleFakeUser> ByCode = new();
    public static readonly ConcurrentDictionary<string, GoogleFakeUser> ByAccessToken = new();
}
