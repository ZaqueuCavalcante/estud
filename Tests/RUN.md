# RUN

dotnet test --filter TestCategory=Integration
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName!~UnitTests"

dotnet test --output Detailed
dotnet test --filter "FullyQualifiedName~IntegrationTests"
dotnet test --output Detailed --filter "FullyQualifiedName~IntegrationTests"

## Code Coverage

dotnet test --coverage --coverage-settings Tests/coverage.settings.xml --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --results-directory ./TestResults

reportgenerator -reports:"./TestResults/coverage.cobertura.xml" -targetdir:"./Tests/Reports" -reporttypes:Html

# Mutation

dotnet tool restore

cd Tests
dotnet stryker

Hoje a config roda **só os unit tests** (`test-case-filter`), mutando `Domain/`,
`Extensions/` e `Commands/` — os unit tests sao puros, entao nao sobem Postgres,
Kestrel nem o banco compartilhado, e `concurrency` pode ser > 1.

Pra colocar os testes de integracao de volta, em `stryker-config.json`:

- tirar o `test-case-filter`
- baixar `concurrency` pra **1** — `BackFactory` fixa a porta 5100,
  `MocksFactory` a 5678 e o banco `estud-tests-db` tem nome fixo, entao dois
  workers do Stryker disputam os tres
- ampliar o `mutate` (ex: `**/Features/**/*Service.cs`)

Um recorte pontual sobrescreve o `mutate` da config:

dotnet stryker -m "**/Extensions/**/*.cs"
