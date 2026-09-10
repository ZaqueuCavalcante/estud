# SSO — Pendências

O que falta no fluxo de Single Sign-On.

## Bugs funcionais

### 1. Scheme não registrado na instância: 500 no challenge, 404 no callback

`Back/Features/Identity/SsoChallenge/SsoChallengeController.cs:27`
· `Back/Features/Identity/SsoChallenge/SsoChallengeService.cs:15`
· `Back/Auth/Managers/SsoSchemeManager.cs:18`
· `Back/Program.cs:42`

Os schemes OIDC vivem em memória, por instância. Cada instância os registra no boot
(`RegisterActiveSsoSchemes`) e depois só quando ela mesma cria ou edita uma configuração. Uma
configuração criada ou editada na instância A só chega à B quando a B reinicia.

Só acontece com mais de uma réplica do `back`. O Caddy manda para
`back.railway.internal:5000` (`Caddyfile:17`), e com réplicas o Railway distribui entre elas sem
sticky session. Com uma réplica só, o registro no boot já cobre o restart.

#### O problema tem três partes

**a) Challenge para scheme ausente → 500.** O `SsoChallengeService` consulta o banco (config ativa
com o domínio) e devolve `OIDC_{publicId}`, mas não verifica se o scheme está registrado nesta
instância. O `Challenge(properties, result.Success)` lança
`InvalidOperationException: No authentication handler is registered for the scheme '...'`, e o
usuário recebe um 500 numa navegação de browser, em vez do redirect `?sso_error=` para o qual o
resto do controller foi desenhado.

**b) Challenge para scheme desatualizado → IdP errado.** Se a configuração for editada na
instância A (authority ou client_id), a B continua com as options antigas e manda o usuário para o
IdP antigo. O `HandleAuthorizationCodeReceived` (`SsoOidcScheme.cs:170`) só corrige o client
secret, e só no momento da troca do code.

**c) Callback numa instância que não conhece o scheme → 404.** O IdP redireciona para
`/identity/sso/callback/{publicId}` (`SsoOidcScheme.cs:68`), e esse request pode cair numa instância
diferente da que fez o challenge. O `AuthenticationMiddleware` só trata callbacks dos schemes
registrados (percorre os `GetRequestHandlerSchemesAsync()`). Se a instância não conhece o scheme, o
request segue para o roteamento, não acha endpoint e devolve 404. Nem chega ao `OnRemoteFailure`.

Consertar só o challenge troca um 500 no início do fluxo por um 404 no fim.

#### Solução

Um único `SsoSchemeManager.EnsureRegistered(config)`, que registra o scheme quando ele estiver
**ausente ou desatualizado** (`IsStale(schemeName, config.UpdatedAt)`), usado em dois pontos:

1. **No challenge.** O `SsoChallengeService` já vai ao banco. Basta trocar o
   `Select(x => x.PublicId)` pela config completa (id, public_id, authority, client_id,
   client_secret, updated_at) e chamar o `EnsureRegistered` antes do `Challenge`. O caso "não
   achou no banco → `?sso_error=`" já existe: o service devolve `SsoNotConfiguredForDomain`.
2. **No callback.** Um middleware pequeno antes do `UseAuthentication()` (`Program.cs:42`), que
   reconhece `/identity/sso/callback/{guid}`, carrega a config com
   `ctx.GetActiveSsoConfigForSchemeAsync(publicId)` e chama o `EnsureRegistered`. Se não achar,
   deixa passar: o request cai no 404 de hoje, que é o certo para uma configuração que não existe
   ou foi desativada.

Os cookies de correlação e de nonce funcionam entre instâncias, porque a DataProtection já usa
chaves no banco (`DataProtection.EntityFrameworkCore`). Com os dois pontos, o
`HandleAuthorizationCodeReceived` fica praticamente redundante.

Um `try/catch` em volta do `Challenge` que redireciona com `?sso_error=` serve como rede de
segurança. Como correção principal não serve: esconde o problema, e o login com SSO continua
falhando em parte dos requests.

#### Cuidados na implementação

- **Concorrência.** O `RegisterScheme` (`SsoSchemeManager.cs:18`) faz `RemoveScheme` e depois
  `AddScheme`, sem atomicidade. Hoje ele roda quase só em create e update. Com registro sob
  demanda, dois logins simultâneos na mesma instância fazem remove, remove, add, add, e o segundo
  `AddScheme` (`SsoSchemeManager.cs:37`) lança erro de scheme já existente. Também há uma janela
  em que o scheme some no meio do challenge de outro usuário. O `EnsureRegistered` precisa de lock
  (um `SemaphoreSlim` basta) com nova checagem de ausente/desatualizado dentro dele, e só deve
  remover e adicionar quando for de fato necessário.
- **Segredo em texto puro.** O `RegisterScheme` descriptografa o `config.ClientSecret` direto no
  objeto recebido (`SsoSchemeManager.cs:23`). Com config carregada por Dapper, como no boot e no
  `GetActiveSsoConfigForSchemeAsync`, não tem problema. Se alguém passar uma entidade rastreada
  pelo EF e depois chamar `SaveChangesAsync` no mesmo contexto, o segredo vai para o banco sem
  criptografia. Carregar a config do challenge com `AsNoTracking()` (ou por Dapper), ou fazer o
  `RegisterScheme` trabalhar numa cópia.

#### Testes de integração

Para simular "outra instância", criar a configuração pelo endpoint e depois tirar o scheme da
memória com `SsoSchemeManager.RemoveScheme(publicId)`, obtido do `_back.Services`. Casos:

- challenge com scheme ausente → redireciona para o IdP (e não 500);
- login completo com scheme ausente antes do challenge → `/home`;
- scheme removido entre o challenge e o callback → callback tratado e login em `/home`;
- config editada (authority/client_id) com o scheme antigo em memória → challenge sai com as
  options novas;
- callback para `publicId` inexistente ou desativado → continua sem login.
