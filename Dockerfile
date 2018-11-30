FROM microsoft/dotnet:2.1-sdk

RUN mkdir /opt/estbot

ADD . /opt/estbot

RUN dotnet publish /opt/estbot/src/Automation --configuration Release --runtime linux-x64

RUN stat /opt/estbot/src/Automation/Release/netcoreapp2.0/publish/Estranged.Automation

ENTRYPOINT ["/opt/estbot/src/Automation/Release/netcoreapp2.0/publish/Estranged.Automation"]
