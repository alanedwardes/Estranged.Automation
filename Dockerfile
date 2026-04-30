FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Automation/Estranged.Automation.csproj src/Automation/
RUN dotnet restore src/Automation/Estranged.Automation.csproj
COPY src/ src/
RUN dotnet publish src/Automation/Estranged.Automation.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Estranged.Automation.dll"]
