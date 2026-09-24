# Remover os schemes de cookie temporário (`SsoTemp` e `SocialTemp`)

Os handlers remotos de login (`AddGoogle` para social login e os `OpenIdConnectHandler` dinâmicos do
SSO) apontam o `SignInScheme` para dois cookies temporários: `SsoTemp` e `SocialTemp`. Eles seguem o
padrão clássico do ASP.NET Identity (callback externo → cookie temporário → rota local lê o cookie,
emite o login de verdade e descarta o cookie).

O código não usa esse fluxo. Todo o trabalho acontece no `OnTicketReceived`, e **todos** os caminhos
de `HandleTicketReceived` e `HandleRemoteFailure` terminam em `context.HandleResponse()`, o que faz o
`RemoteAuthenticationHandler` pular o `SignInAsync(SignInScheme, ...)`. Resultado:

- o handler de cookie desses schemes nunca é instanciado;
- o lambda de opções do `AddCookie` nunca roda. É por isso que as 4 linhas de cada scheme aparecem sem
  cobertura no report;
- nenhum outro código usa os schemes: as policies só usam `JwtBearerScheme`, e não há
  `AuthenticateAsync`, `SignInAsync` nem `SignOutAsync` com eles.

A proposta é remover os schemes e também o `SignInScheme` que aponta para eles, deixando o
`SignInScheme` dos handlers remotos como `null`.

## Mudanças

### 1. Apagar os arquivos

- `Back/Auth/Schemes/SsoTempScheme.cs`
- `Back/Auth/Schemes/SocialTempScheme.cs`

As constantes `Cookie` (`X-Estud-SsoTempCookie`, `X-Estud-SocialTempCookie`) não são referenciadas
em nenhum outro lugar do Back nem do Web.

### 2. `Back/Configs/AuthenticationConfigs.cs`

Remover as duas chamadas da cadeia:

```csharp
builder.Services
    .AddAuthentication(options => options.DefaultChallengeScheme = JwtBearerScheme.Name)
    .AddJwtBearerScheme(builder.Configuration)
    .AddTwoFactorSetupScheme()
    .AddSsoOpenIdConnectScheme()
    .AddSocialLoginSchemes(builder.Configuration);
```

### 3. `Back/Auth/Schemes/SsoOidcScheme.cs`

Em `ConfigureSsoSchemeOptions`, remover:

```csharp
options.SignInScheme = SsoTempScheme.Name;
```

### 4. `Back/Auth/Schemes/SocialLoginScheme.cs`

Em `AddSocialLoginSchemes`, remover:

```csharp
options.SignInScheme = SocialTempScheme.Name;
```

## O que acontece com o `SignInScheme` depois da remoção

**SSO (OIDC).** O `SsoSchemeManager.RegisterScheme` monta o `OpenIdConnectOptions` na mão
(`new OpenIdConnectOptions()` + `ConfigureSsoSchemeOptions` + os `IPostConfigureOptions` registrados)
e coloca direto no `IOptionsMonitorCache`. Esse caminho não passa pelo `OptionsFactory`, então:

- o `EnsureSignInScheme` (post-configure que o `AddRemoteScheme` registraria) não participa, porque o
  SSO não usa `AddRemoteScheme`;
- o `Validate()` das opções não é chamado.

O `SignInScheme` simplesmente fica `null`. Não há validação no startup nem no primeiro request.

**Google.** O `AddGoogle` passa pelo `AddRemoteScheme`, que registra o `EnsureSignInScheme`. Ele
preenche `SignInScheme ??= DefaultSignInScheme ?? DefaultScheme`. Os dois são `null` aqui (só o
`DefaultChallengeScheme` é definido), então o `SignInScheme` continua `null`.

As opções do Google são montadas pelo `OptionsFactory` quando o scheme é resolvido pela primeira vez.
O `AuthenticationMiddleware` faz isso em todo request, porque o Google é um
`IAuthenticationRequestHandler`. Nesse momento roda o `Validate`, e o
`RemoteAuthenticationOptions.Validate(scheme)` só barra `SignInScheme == scheme`, não `null`.
**Esse é o ponto principal a confirmar** (ver validação).

## Comportamento em produção

| Cenário | Antes | Depois |
|---|---|---|
| Startup | sobe | sobe |
| Challenge (redirect ao IdP/Google) | funciona | igual |
| Callback, qualquer caminho atual | `HandleResponse()`, sem sign-in | igual |
| Callback, caminho futuro que esqueça o `HandleResponse()` | grava cookie temporário e segue um fluxo sem destino (usuário não recebe JWT, falha silenciosa) | `InvalidOperationException: No authenticationScheme was specified, and there was no DefaultSignInScheme found` → 500, visível no log e na telemetria |

A última linha é a única diferença de comportamento, e troca uma falha silenciosa por uma explícita.

## Validação

Nenhum teste referencia `SsoTempScheme` ou `SocialTempScheme`, então nenhum teste precisa mudar.

1. **Build**: `dotnet build`.
2. **Startup + Google**: subir o Back e fazer qualquer request. O `AuthenticationMiddleware` resolve
   o handler do Google em todo request, o que força a montagem e o `Validate` das opções dele. Se o
   `SignInScheme` `null` fosse barrado, estouraria aqui.
3. **Social login (challenge)**:
   `dotnet test --filter "FullyQualifiedName~SocialLogin|FullyQualifiedName~GoogleOneTap"`.
4. **SSO ponta a ponta (callback OIDC real via Keycloak)**:
   `dotnet test --filter "FullyQualifiedName~Sso"`. O `SsoLoginKeycloakIntegrationTests` passa pelo
   callback completo e pelo `HandleTicketReceived`.
5. **Suíte completa**: `dotnet test`.
6. **Google callback (manual, em dev)**: os testes cobrem só o challenge do Google, não o callback.
   Um login com Google em dev confirma que o callback segue funcionando sem o scheme temporário.

## Rollback

A mudança é só de remoção. Um `git revert` do commit restaura o comportamento anterior.
