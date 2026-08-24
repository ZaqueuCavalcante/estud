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

cd tests
dotnet stryker -o
