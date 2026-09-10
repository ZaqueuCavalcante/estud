# SSO — Pendências

O que falta no fluxo de Single Sign-On. Os bloqueadores do login e a cobertura de testes do
challenge/callback já foram resolvidos; o que segue continua em aberto.

Verificado contra o código em 2026-09-10.

---

## Bugs funcionais

### 1. Challenge para scheme não registrado devolve 500

`Back/Features/Identity/SsoChallenge/SsoChallengeController.cs:27`

`Challenge(properties, result.Success)` é chamado sem verificar se o scheme está registrado nesta
instância. O `AuthenticationService.ChallengeAsync` lança
`InvalidOperationException: No authentication handler is registered for the scheme '...'`, então o
usuário leva um 500 cru numa navegação de browser, em vez do redirect `?sso_error=` para o qual o
resto do controller foi desenhado.

O registro no boot resolveu o caso do restart, mas não o multi-instância: uma configuração criada
na instância A só existe na B depois que a B reinicia.

Para resolver isso, verifique no banco se esta tudo ok com o scheme.
Se o scheme n tiver registrado ainda, va no banco buscar por ele e registre se achar.
Caso contrario deve retornar com `?sso_error=` pro usuario.
