using System.Collections.Concurrent;

namespace Estud.Mocks.SocialLogin.Google;

public record GoogleMockUser(
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
public static class GoogleMockUsers
{
    public static readonly ConcurrentDictionary<string, GoogleMockUser> ByCode = new();
    public static readonly ConcurrentDictionary<string, GoogleMockUser> ByAccessToken = new();
}
