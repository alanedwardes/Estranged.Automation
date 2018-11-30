FROM microsoft/dotnet:2.1-sdk

RUN cd src/Automation

RUN dotnet publish --configuration Release --runtime linux-x64

ENTRYPOINT ["src/Automation/Release/netcoreapp2.0/publish/Estranged.Automation"]
