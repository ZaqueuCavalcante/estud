# RUN

dotnet test --filter TestCategory=Integration
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName!~UnitTests"
dotnet test --filter "FullyQualifiedName~Calendar_CreateCalendarDay_Should_not_create_day_that_already_exists"
dotnet test --filter "FullyQualifiedName~Dev_Should_create_initial_institution_data_for_easy_development_debugging"


dotnet test --output Detailed
dotnet test --filter "FullyQualifiedName~IntegrationTests"
dotnet test --output Detailed --filter "FullyQualifiedName~IntegrationTests"

## Code Coverage

dotnet test --coverage --coverage-settings Tests/coverage.settings.xml --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --results-directory ./TestResults

reportgenerator -reports:"./TestResults/coverage.cobertura.xml" -targetdir:"./Tests/Reports" -reporttypes:Html

# Mutation

Roda localmente (só unit tests, mutando só o código que eles cobrem) e publica em https://zaqueucavalcante.github.io/estud/mutation,
atualizando a badge do README:

.\Scripts\mutation-tests.ps1

Só gerar o relatório, sem publicar:

.\Scripts\mutation-tests.ps1 -NoPublish

Argumentos extras vão direto pro Stryker; um recorte pontual sobrescreve o `mutate` da config:

.\Scripts\mutation-tests.ps1 -NoPublish -m "**/Extensions/**/*.cs"

Não precisa de Docker: o `test-case-filter` deixa só os `*UnitTests`, então o Stryker usa a concorrência padrão
(metade dos núcleos). Ao criar unit tests pra um arquivo novo, adicione-o ao `mutate` do `stryker-config.json`.
