# Estud contributing-friendly

Como deixar o Estud fácil de contribuir: que alguém saia do `git clone` e chegue num PR aceito sem
precisar perguntar nada.

O maior atrito hoje é **rodar o projeto localmente**. Não existe um caminho documentado do clone
até a app de pé. Resolvido isso, o resto são melhorias incrementais.

## O que já existe

- Licença MIT, README com visuais e badges de cobertura (linha/branch) e mutation score.
- Arquitetura clara (vertical slices em `Back/Features/`) com convenções bem definidas, só que
  escritas no `CLAUDE.md`, voltadas pro agente.
- CI rodando os testes em todo PR (`.github/workflows/pr.tests.yml`), com Postgres como service.
- `Plans/` e `TODO.md` mostram para onde o projeto vai. O próprio `TODO.md` já lista "guia de
  contribuição no github, padronização de issues e PRs".
- Nenhum segredo versionado: o `appsettings.Development.json` (que tem o client secret do Google)
  está fora do git, e o `appsettings.Copy.json` usa valores falsos.

## Prioridade alta: rodar localmente

### 1. `docker-compose.yml`

O `CLAUDE.md` manda rodar `docker-compose up`, mas **não existe compose no repo**. Criar um com:

- Postgres (obrigatório, é o que o back e os testes precisam);
- Back, Web e `Mocks`, para quem quer só subir tudo e olhar.

### 2. Template de settings

`Back/appsettings.Copy.json` não diz o que é. Renomear para
`appsettings.Development.example.json` e documentar:

```bash
cp Back/appsettings.Development.example.json Back/appsettings.Development.json
```

Deixar claro quais integrações (Google, e-mail/Brevo, S3) são opcionais e podem ficar desligadas
em dev.

### 3. `CONTRIBUTING.md`

Na raiz, cobrindo:

- pré-requisitos: .NET 10.0.200 (`global.json`), Node + pnpm, Postgres;
- setup do back, do front (`Web/`) e do projeto `Mocks`: o que ele simula e quando subir;
- como rodar os testes, com o aviso de que eles apagam e recriam o banco local
  `estud-tests-db`;
- como criar migrations;
- tour rápido pela arquitetura: vertical slices, Result Pattern, Commands assíncronos.

### 4. Extrair as convenções do `CLAUDE.md`

Quase tudo que está no `CLAUDE.md` vale para humanos também:

- `HasValue()` / `IsEmpty()` em vez de `string.IsNullOrEmpty`;
- enums com valor inteiro explícito;
- LINQ só em method syntax;
- SQL com palavras-chave em maiúsculo;
- XML docs multi-linha nos controllers;
- handlers de evento com arrow function;
- `#region` fixos nos testes de integração, cenário montado via API.

Mover para `docs/CONVENTIONS.md` (ou uma seção do `CONTRIBUTING.md`) e deixar o `CLAUDE.md`
apontando para lá. Fonte única, sem duas cópias divergindo.

## Prioridade média: padronizar o GitHub

### 5. Templates de issue e PR

Em `.github/`:

- `ISSUE_TEMPLATE/bug.yml` e `ISSUE_TEMPLATE/feature.yml`, em formato de formulário;
- `PULL_REQUEST_TEMPLATE.md` com checklist:
  - [ ] testes de integração adicionados
  - [ ] migration gerada (se mexeu em entidade)
  - [ ] exemplos do Scalar preenchidos (`GetExamples()`, `ErrorExamplesProvider`)

### 6. Labels de entrada

Criar `good first issue` e `help wanted`. Os itens do `TODO.md` e do `Plans/` viram issues
naturalmente. Bons candidatos a primeira contribuição:

- header `X-Estud-Signature` nos webhooks;
- nomes inteiros no breadcrumbs, reduzindo só no mobile.

### 7. Formatação automática no CI

Só existe `.editorconfig` em `Web/`. Criar um na raiz com as regras de C# e rodar no workflow de
PR:

- `dotnet format --verify-no-changes`
- `pnpm lint`

As convenções passam a ser cobradas pela máquina, não pelo review.

## Prioridade baixa: extras

9. `CODE_OF_CONDUCT.md` (Contributor Covenant) e `SECURITY.md`. O `SECURITY.md` pesa mais: o
   sistema tem SSO, 2FA e dados de alunos, então precisa de um canal privado para reportar
   vulnerabilidades.
10. Ativar GitHub Discussions para dúvidas, para que elas não virem issues.
11. Devcontainer (`.devcontainer/`) com .NET, Node e Postgres, para setup em um clique no
    Codespaces.
12. Seção "Contribuindo" no README, com link para o guia.

## Ordem sugerida

1. Compose + template de settings (itens 1 e 2): sem isso o resto não adianta.
2. `CONTRIBUTING.md` + convenções extraídas (itens 3 e 4).
3. Templates de issue/PR + labels (itens 5 e 6), já abrindo as primeiras `good first issue`.
4. Lockfile e formatação no CI (itens 7 e 8).
5. Extras conforme o projeto ganhar contribuidores.
