# GoogleService testado de verdade

Testar a validação do id_token do Google One Tap de ponta a ponta e remover o `FakeGoogleService`.

Hoje o `FakeGoogleService` substitui o `IGoogleService` inteiro nos testes
(`Back/Configs/ServicesConfigs.cs`). Os testes de `GoogleOneTapLogin` cobrem bem a regra de negócio
(vínculo, auto-provisionamento, SSO obrigatório), mas o `GoogleService` real nunca roda: assinatura,
`aud`, `iss` e expiração do token não têm nenhum teste.

## Ideia

Não é preciso trocar a lib do Google. O `GoogleJsonWebSignature.ValidateAsync` é só um atalho para
`JsonWebSignature.VerifySignedTokenAsync<GoogleJsonWebSignature.Payload>(token, SignedTokenVerificationOptions)`,
cujas options aceitam `CertificatesUrl`, `TrustedIssuers` e `TrustedAudiences`.

A lib não confere se o `iss` bate com a URL dos certificados, então o `Fakes` pode emitir tokens com
`iss = https://accounts.google.com`. A única coisa configurável passa a ser a URL do JWKS.

> Antes de começar: confirmar que a versão do `Google.Apis.Auth` usada no projeto expõe
> `SignedTokenVerificationOptions.CertificatesUrl` publicamente.

## Passos

### 1. `GoogleService`

- Trocar `GoogleJsonWebSignature.ValidateAsync` por `JsonWebSignature.VerifySignedTokenAsync`.
- `CertificatesUrl` vem de `SocialLogin:Google:CertificatesUrl`. Sem essa config, usa o padrão do
  Google (`https://www.googleapis.com/oauth2/v3/certs`).
- `TrustedIssuers` (`accounts.google.com` e `https://accounts.google.com`) e `TrustedAudiences`
  (client id) fixos no código.
- Configurar só no `appsettings.Testing.json`, apontando para o `Fakes`. Segue o mesmo padrão do
  `OverrideWith` dos endpoints de OAuth.

### 2. `Fakes`

- `GET social-login/google/certs`: JWKS. Reaproveitar o `OidcFakeKeys` ou criar uma chave própria.
- Um endpoint que emite a `credential` do One Tap já assinada, com parâmetros para email,
  `email_verified`, name, sub, aud e exp.
- Opções para gerar tokens inválidos: assinado com outra chave, `aud` errado, expirado, `iss` errado.

### 3. Testes

- `FakeGoogleService.SeedGoogleToken(...)` vira algo como
  `await FakesFactory.GoogleOneTapCredential(email, ...)`. Os testes atuais continuam iguais em
  comportamento.
- Testes novos de rejeição: assinatura inválida, audience de outro client, token expirado, issuer que
  não é do Google e token malformado.

### 4. Limpeza

- Remover o `FakeGoogleService` e o `Replace` no `ServicesConfigs.cs`.
- Provavelmente remover o `IGoogleService`, que existe só para permitir a troca, e injetar o
  `GoogleService` direto.

### Cuidados

- A lib guarda os certificados em cache por URL. Como a chave do fake é fixa, isso não atrapalha.
- No teste de token expirado, gerar o `exp` bem no passado (por exemplo, 1h atrás) para ficar fora da
  tolerância de relógio.

## Prós

- **Testa a validação de verdade.** Hoje, se alguém passar o `expectedAudience` errado ou trocar a
  validação por um simples decode do JWT, nenhum teste quebra. Isso vira uma brecha de segurança:
  qualquer token do Google, emitido para qualquer app, seria aceito.
- **Tira código de teste do `Back`.** O `FakeGoogleService`, com um dicionário estático, sai do
  assembly que vai para produção, junto com o `Replace` condicional no DI.
- **Mesmo caminho em teste e em produção.** Fica igual ao login OAuth e ao SSO, que já são testados
  assim.
- **Consistência com o resto do `Fakes`.** O OIDC já emite JWT assinado com JWKS.
- **Menos abstração.** O `IGoogleService` provavelmente deixa de ser necessário.

## Contras

- **Depende de uma API mais baixa da lib.** `VerifySignedTokenAsync` é menos usada e menos
  documentada que o `ValidateAsync`. Se uma versão futura mudar ou esconder o `CertificatesUrl`, é
  preciso adaptar.
- **Os issuers ficam escritos no nosso código.** Hoje a lib decide sozinha quais aceitar. Se o Google
  mudar algo, a lib não nos cobre automaticamente.
- **Uma config nova** (`CertificatesUrl`). Se for configurada errado em produção, todo login One Tap
  falha. O risco é baixo, porque o padrão é a URL do Google e só o Testing sobrescreve.
- **Mais código no `Fakes`.** Endpoint de certs, endpoint de emissão e opções de token inválido: umas
  60 a 80 linhas.
- **Testes um pouco mais lentos.** Cada teste faz uma chamada HTTP extra ao fake para gerar o token, e
  o back busca o JWKS (que fica em cache).

## Fora do escopo

Continua sem cobertura o que só o Google real valida: client id e origins no Google Console, e o
comportamento do One Tap/FedCM no navegador. Para isso, um smoke test manual ou E2E num ambiente de
staging.
