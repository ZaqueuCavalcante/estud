# RUN

dotnet test --filter TestCategory=Integration
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName!~UnitTests"
dotnet test --filter "FullyQualifiedName~Courses_CreateCourse_Should_not_create_course_when_not_authenticated" --output Detailed
dotnet test --filter "FullyQualifiedName~Dev_Should_create_initial_institution_data_for_easy_development_debugging"


dotnet test --output Detailed
dotnet test --filter "FullyQualifiedName~IntegrationTests"
dotnet test --output Detailed --filter "FullyQualifiedName~IntegrationTests"

## Code Coverage

dotnet test --coverage --coverage-settings Tests/coverage.settings.xml --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --results-directory ./TestResults

reportgenerator -reports:"./TestResults/coverage.cobertura.xml" -targetdir:"./Tests/Reports" -reporttypes:Html

# Mutation

Roda localmente (unit + integration tests) e publica em https://zaqueucavalcante.github.io/estud/mutation,
atualizando a badge do README:

.\Scripts\mutation-tests.ps1

Só gerar o relatório, sem publicar:

.\Scripts\mutation-tests.ps1 -NoPublish

Argumentos extras vão direto pro Stryker; um recorte pontual sobrescreve o `mutate` da config:

.\Scripts\mutation-tests.ps1 -NoPublish -m "**/Extensions/**/*.cs"

Precisa do Docker de pé (Testcontainers). `concurrency` fica em **1**: `BackFactory` fixa a porta 5100,
`FakesFactory` a 5678 e o banco `estud-tests-db` tem nome fixo, então dois workers do Stryker disputam os três.
