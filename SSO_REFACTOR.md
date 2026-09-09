# SSO — Pendências do fluxo de setup + login

Levantamento do estado do fluxo de Single Sign-On (setup da configuração + login via OIDC),
feito sobre o `master` em 2026-09-08.

Os bloqueadores que impediam o login de funcionar já foram corrigidos. O que segue abaixo são os
bugs e riscos que permanecem em aberto.

---

## Mapa do fluxo

### Setup

| Etapa | Arquivo |
|---|---|
| Tela de configuração | `Web/app/pages/security/sso.vue` |
| Modais de criação/edição | `Web/app/components/security/sso/{Add,Edit}SsoConfigurationModal.vue` |
| `POST /identity/sso/configurations` | `Back/Features/Identity/CreateSsoConfiguration/` |
| `PUT /identity/sso/configurations/{id}` | `Back/Features/Identity/UpdateSsoConfiguration/` |
| `GET /identity/sso/configuration` | `Back/Features/Identity/GetSsoConfiguration/` |
| Validação de authority (anti-SSRF) | `Back/Extensions/SsoExtensions.cs` |
| Cifra do client secret (DataProtection) | `Back/Auth/Managers/SsoEncryptionManager.cs` |
| Registro do scheme OIDC em memória | `Back/Auth/Managers/SsoSchemeManager.cs` |

### Login

```
[login] --POST /identity/sso/check-availability--> { ssoEnabled, ssoRequired, providerType }
        --GET  /identity/sso/challenge?email=...--> Challenge("OIDC_{publicId}")
        --> IdP --> GET /api/identity/sso/callback/{publicId}
        --> OnAuthorizationCodeReceived (refresh do secret)
        --> OnTicketReceived (resolve tenant, acha user, emite JWT)
        --> redirect para {frontend}/home
```

| Etapa | Arquivo |
|---|---|
| Tela de login | `Web/app/pages/login/index.vue` |
| `POST /identity/sso/check-availability` | `Back/Features/Identity/CheckSsoAvailability/` |
| `GET /identity/sso/challenge` | `Back/Features/Identity/SsoChallenge/` |
| Handler OIDC + callback | `Back/Auth/Schemes/SsoOidcScheme.cs` |
| Registro dos schemes no boot | `Back/Configs/AuthenticationConfigs.cs` → `UseSsoSchemes()` |
| Cookie temporário do sign-in scheme | `Back/Auth/Schemes/SsoTempScheme.cs` |
| Emissão do JWT | `Back/Features/Identity/SignIn/SignInService.cs` |

---

## Bugs funcionais

### 1. `RequireSso` é ignorado na criação

`Back/Features/Identity/CreateSsoConfiguration/CreateSsoConfigurationService.cs:41`

`CreateSsoConfigurationIn.RequireSso` existe e o modal `AddSsoConfigurationModal.vue:39` envia o
valor, mas o construtor `SsoConfiguration(...)` hardcoda `RequireSso = false` e `data.RequireSso`
nunca é lido. O usuário marca "SSO Obrigatório" no cadastro e nada acontece — só funciona depois de
abrir o modal de edição e salvar de novo.

Isso desativa na prática todo o caminho obrigatório já implementado na tela de login (campo de
senha escondido, login com Google escondido, `SsoLoginRequired` no `email-password-login`).

### 2. `EmailConfirmed = true` nunca é persistido

`Back/Auth/Schemes/SsoOidcScheme.cs:144`

`user` vem tracked de `ctx.Users.FirstOrDefaultAsync`, é mutado, e nada chama `SaveChangesAsync`.
`SignInService.SignIn` só lê e emite o JWT. Compare com o caminho social
(`SocialLoginScheme.cs:110-112`), que faz `SaveChangesAsync` explicitamente. Hoje é código morto.

### 3. `SignIn(email)` resolve o usuário sem a instituição

`Back/Features/Identity/SignIn/SignInService.cs:17`

O `HandleTicketReceived` faz o lookup correto —
`u.Email == email && u.InstitutionId == institutionId` (`SsoOidcScheme.cs:133`) — e depois joga o
resultado fora, chamando `SignIn(email)`, que refaz a busca com
`Where(u => u.Email == email).FirstAsync()`.

Se o mesmo email existir em duas instituições, o JWT sai com a instituição errada. Vale para o
caminho social também, mas no SSO é mais grave porque a config já resolveu o tenant corretamente
logo acima.

---

## Pontos de decisão / risco

### 4. SSO pula o 2FA por completo

`EmailPasswordLoginService.cs:31-42` trata `user.TwoFactorEnabled` e `role.TwoFactorRequired`. O
`HandleTicketReceived` vai direto para o `SignIn`. Pode ser intencional (o IdP faz o MFA), mas hoje
um role com `TwoFactorRequired = true` é contornável via SSO.

### 5. `RegisterScheme` muta a entidade tracked

`Back/Auth/Managers/SsoSchemeManager.cs:24` faz
`config.ClientSecret = encryption.Decrypt(config.ClientSecret)`. Em `CreateSsoConfigurationService`
e `UpdateSsoConfigurationService` o `config` está tracked no `EstudDbContext` scoped da request.
Hoje não há `SaveChanges` depois, então não vaza — mas qualquer save posterior no mesmo escopo
gravaria o secret em plaintext. Passar o secret decifrado como parâmetro, em vez de mutar a
entidade, elimina o risco.

### 6. Nenhuma restrição de configuração única por instituição

`CreateSsoConfigurationService` só checa duplicidade de **domínio**;
`GetSsoConfigurationService` retorna `FirstOrDefaultAsync` filtrando por instituição. Dois diretores
com domínios de email diferentes criam duas configurações na mesma instituição e a segunda fica
invisível na tela.

### 7. Cobertura de teste

`Tests/Features/Identity/SsoChallenge/` e `Tests/Features/Identity/SsoLogin/` estão **vazias**.

Os quatro arquivos existentes (`CheckSsoAvailability`, `CreateSsoConfiguration`,
`GetSsoConfiguration`, `UpdateSsoConfiguration`) cobrem só o CRUD e a checagem de disponibilidade —
nada do challenge nem do callback. Os bloqueadores já corrigidos (coluna `external_id` inexistente
e schemes não registrados no boot) passaram despercebidos exatamente por isso, e os bugs 2 e 3
acima continuam sem nenhum teste que os pegue.

### 8. Menores

- `HandleRemoteFailure` (`SsoOidcScheme.cs:84-87`) é `async` sem `await` (CS1998) e resolve um
  `EstudDbContext` que não usa.
- `CheckSsoAvailabilityController` não tem rate limiter, ao contrário do `SsoChallengeController`
  (`SensitivePolicy`) — é um endpoint público que permite enumerar quais domínios têm SSO.
