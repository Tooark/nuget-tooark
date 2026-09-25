#!/bin/bash
#
# Exemplo de análise local com SonarQube. Requer as ferramentas globais:
#   dotnet tool install -g dotnet-sonarscanner
#   dotnet tool install -g dotnet-coverage
#
# A cobertura é coletada pelo dotnet-coverage em volta do "dotnet test", no formato XML que o SonarQube lê
# (sonar.cs.vscoveragexml.reportsPaths). Nenhum parâmetro do coverlet entra aqui: seriam dois coletores
# gravando o mesmo arquivo.

# Defina as variáveis do projeto e token do SonarQube
sonarProjectKey="Tooark"
sonarHost="<HOST-SONARQUBE>"
sonarToken="<TOKEN-SONARQUBE>"

# Inicie a análise do SonarQube
dotnet sonarscanner begin /k:$sonarProjectKey /d:sonar.host.url=$sonarHost /d:sonar.token=$sonarToken /d:sonar.scanner.scanAll=false /d:sonar.cs.vscoveragexml.reportsPaths="coverage.xml"

# Construa o projeto sem incremental
dotnet build --no-incremental

# Colete a cobertura de código
dotnet-coverage collect "dotnet test --project Tooark.Tests/Tooark.Tests.csproj --no-build" -f xml -o "coverage.xml"

# Finalize a análise do SonarQube
dotnet sonarscanner end /d:sonar.token=$sonarToken
