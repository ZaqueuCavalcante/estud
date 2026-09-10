# SSO — Pendências

O que falta no fluxo de Single Sign-On. Os bloqueadores do login e a cobertura de testes do
challenge/callback já foram resolvidos; o que segue continua em aberto.

Verificado contra o código em 2026-09-10.

---

## Bugs funcionais

### 1. `RequireSso` é ignorado na criação

`Back/Features/Identity/CreateSsoConfiguration/CreateSsoConfigurationService.cs:41`
· `Back/Domain/Identity/SsoConfiguration.cs:46`

`CreateSsoConfigurationIn.RequireSso` existe e o modal `AddSsoConfigurationModal.vue:39` envia o
valor, mas o construtor `SsoConfiguration(...)` hardcoda `RequireSso = false` e `data.RequireSso`
nunca é lido. O usuário marca "SSO Obrigatório" no cadastro e nada acontece — só funciona depois de
abrir o modal de edição e salvar de novo.

Isso desativa na prática todo o caminho obrigatório já implementado na tela de login (campo de
senha escondido, login com Google escondido, `SsoLoginRequired` no `email-password-login`).

### 2. `EmailConfirmed = true` nunca é persistido

`Back/Auth/Schemes/SsoOidcScheme.cs:152`

`user` vem tracked de `ctx.Users.FirstOrDefaultAsync` (linha 140), é mutado, e nada chama
`SaveChangesAsync` antes do `SignIn` da linha 156. `SignInService.SignIn` só lê e emite o JWT.
Compare com o caminho social (`SocialLoginScheme.cs:110-112`), que faz `SaveChangesAsync`
explicitamente. Hoje é código morto.

### 3. O e-mail devolvido pelo IdP não é normalizado

`Back/Auth/Schemes/SsoOidcScheme.cs:110`

Só o **domínio** é passado por `ToLowerInvariant()` (linha 119); o e-mail em si vai cru para o
`u.Email == email` da linha 140, e a comparação no Postgres é case-sensitive. Um IdP que devolva
`Diretor@empresa.com` para um usuário gravado como `diretor@empresa.com` cai em
`SsoLoginUserNotFound`.

O caminho social não tem esse problema porque faz `email = email!.ToLowerInvariant()`
(`SocialLoginScheme.cs:74`) logo após extrair a claim.

### 6. Challenge para scheme não registrado devolve 500

`Back/Features/Identity/SsoChallenge/SsoChallengeController.cs:27`

`Challenge(properties, result.Success)` é chamado sem verificar se o scheme está registrado nesta
instância. O `AuthenticationService.ChallengeAsync` lança
`InvalidOperationException: No authentication handler is registered for the scheme '...'`, então o
usuário leva um 500 cru numa navegação de browser, em vez do redirect `?sso_error=` para o qual o
resto do controller foi desenhado.

O registro no boot resolveu o caso do restart, mas não o multi-instância: uma configuração criada
na instância A só existe na B depois que a B reinicia.

### 7. Nenhuma restrição de configuração única por instituição

`CreateSsoConfigurationService:38` só checa duplicidade de **domínio**;
`GetSsoConfigurationService` retorna `FirstOrDefaultAsync` filtrando por instituição. Dois diretores
com domínios de email diferentes criam duas configurações na mesma instituição e a segunda fica
invisível na tela.

---

## Menores

- `SsoSchemeManager.UpdateScheme` (`SsoSchemeManager.cs:49`) é código morto: ninguém chama, e o
  corpo faz `RemoveScheme` + `RegisterScheme` quando `RegisterScheme` já começa com `RemoveScheme`.
- `HandleRemoteFailure` (`SsoOidcScheme.cs:92-99`) é `async` sem `await` (CS1998) e resolve um
  `EstudDbContext` que não usa.
- `CheckSsoAvailabilityController` não tem rate limiter, ao contrário do `SsoChallengeController`
  (`SensitivePolicy`) — é um endpoint público que permite enumerar quais domínios têm SSO.

---

## Cobertura de teste restante

`SsoChallengeIntegrationTests` (4 testes) e `SsoLoginIntegrationTests` (11 testes) cobrem o
challenge, o callback OIDC completo contra o IdP de mock (`Mocks/Oidc/`), os erros do provedor e o
isolamento entre instituições.

Continuam sem teste:

- **`RequireSso` ignorado na criação** — nenhum teste cria uma configuração com `requireSso: true`
  e confere o efeito.
- **`EmailConfirmed` não persistido** — `Should_login_another_user_of_the_same_institution` usa um
  professor com `EmailConfirmed = false` e seria o cenário natural para o assert, mas ele foi
  deixado de fora porque falharia. Adicionar junto com a correção.
- **E-mail do IdP não normalizado** — falta o equivalente ao
  `Should_match_existing_user_ignoring_email_case` que já existe no caminho social.
