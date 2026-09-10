using System.Text;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Estud.Mocks.Oidc;

/// <summary>
/// Par de chaves RSA do IdP de mock: assina os id_tokens e alimenta o JWKS.
/// </summary>
public static class OidcMockKeys
{
    // Chave fixa, como a de um IdP real: o JWKS e a assinatura dos id_tokens não mudam
    // entre execuções, então um token capturado num teste continua verificável depois.
    private const string PrivateKeyPem = """
        -----BEGIN PRIVATE KEY-----
        MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQC8/xldbRfc0ZVB
        jkq8BN4KWqNil6fO0vb1krIGTbnxIXHrGukyhfWo+PV+F7zgZexONn3LxJU7yX0c
        XPz41B2ODXJkQ21HFbG7kcEI4Pg+XdLNn+/252IUrHwyEDlL7puJV72gzH0bZ43y
        ZHVfAzzd7r8jv0SnZ/cN/08zychVy/rh0irynvFZkYczN0+oHj9EK0w/jPHo1LdY
        8ce8mkqjpOVixSSPfPH9aZ2iqnJjeISXlVI2vvUfCBQrs9Mgzpo1jcSLhgSzb7zd
        JMcAkBq2ot/CNiDAoOwRP8Sdbnz6UFnlQjw0E8YQzMswwUv7B3vC+j2veAPOIl2q
        /rq9xLctAgMBAAECggEAdyLXem0qfagfzhNESQFIdn5sP9oZjeauhl7SmealL7tF
        dE6icZbAJKPLHJEaHtog+6yd37Ur3WRF2XtEfBY6CzGuykU2vXiPcQ2QAWjPE4FR
        QQ1w1tNEoIOaRnSzqYnfwdPtVU+SDZRZhlKNKjeARuYY1w/a1yxSMCCgbKTmBC5a
        hAJnHiHoHlUi1jwd61hX7OgMEY72akgGKhPpm27AXcF2S8bOffULa18cqf1g1vHg
        jgBaiMH01HVXFV28VNmzr4Z+zOqPlCG6EraLP54DDSL28EAaf1ah98jOElVtrtUG
        sVD4lfW7yUqxWE19we+dVW6eccnWrSuCKx9mFbhCgQKBgQDx+TpHPoAA+AyclL9n
        Yp/RrsE311uzf+rZsTVfNebw51yAGV2o2Sdbmj9gGEepbVwUeaZJWBBUKjle3WNx
        kBbAoKkrXIdQevsYRuqIHWTbBl5+SzrOYpTgIj0yyCRzOsj5rtDfPXEo818GcY3c
        MxOZzAh0nynRn2o/aGLRagr41wKBgQDH87eMQHVbdZBpD7Oq86hyT2qFFCVL75Dt
        aB6kHj76FFCZjghWNG24sKIo3IRtnovu1Cl6op7HrvG8Lg+gghskrbdMUqFUDhyC
        8rqOREbLncnPANpWiPibA5Do6klxbjXw7esOMm6M3Fj7D7ddcTbpRTKBRy6dUWhO
        5pcLsx27mwKBgQDReZ+hE5M+w34vg3obpz6SCIZOsEo7n66RHJ8GuKQfwzrJzqvN
        Q4iV/XeF2h8XpovDUfjJn8orAo69+ExhgIqh4bPxzN17p6t+Pc5FXaT1E5N0I+5Z
        wu/9BMcUEj2z350iwdsil49CE0YdTuqvSSxbxU1AoJVUWnxhPh3mCrZK6wKBgQC9
        YWBfPk3pKhh06aJKMC1C12UUVlhc67JgqVUcLGmJguQ0DAppW47wdpugB/yFtrzi
        n6AJvyyUBGaAzT+PzqrWupH5f+m9KwBmJm/7fz9uayxRG4WwoFqWt3HwqLaW8MO2
        RiFzeOCsGadNYz1RC4HuvtNvDnRgHFKnKE+3jRaEMQKBgQClth0buLugnNhV1JcL
        LlnOeIsBYc16B1Gnht4KjtqOM6ZDy4l7P8/W4TcYa3eSmjx9Ncu68fOESNdMP/F/
        e5c2R8a4giQ9GrKf4ygf+iJ6Sn06y2RegEQAqdU922w+YQVQxKiJQ86lihUrHxgx
        qFnAkVrr4oKlqLvoAovLGclyvg==
        -----END PRIVATE KEY-----
        """;

    private static readonly RSA Rsa = CreateRsa();

    private static readonly RSAParameters PublicParameters = Rsa.ExportParameters(false);

    public static readonly string KeyId = BuildThumbprint(PublicParameters);

    public static SigningCredentials SigningCredentials =>
        new(new RsaSecurityKey(Rsa) { KeyId = KeyId }, SecurityAlgorithms.RsaSha256);

    public static object PublicJwk() =>
        new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid = KeyId,
            n = Base64UrlEncoder.Encode(PublicParameters.Modulus),
            e = Base64UrlEncoder.Encode(PublicParameters.Exponent),
        };

    private static RSA CreateRsa()
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(PrivateKeyPem);
        return rsa;
    }

    /// <summary>
    /// JWK Thumbprint (RFC 7638), o mesmo formato de kid que Google, Azure AD e Okta publicam.
    /// </summary>
    private static string BuildThumbprint(RSAParameters parameters)
    {
        var n = Base64UrlEncoder.Encode(parameters.Modulus);
        var e = Base64UrlEncoder.Encode(parameters.Exponent);

        var canonical = $$"""{"e":"{{e}}","kty":"RSA","n":"{{n}}"}""";

        return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
