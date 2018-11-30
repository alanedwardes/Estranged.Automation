FROM microsoft/dotnet:2.1-sdk

RUN mkdir /opt/estbot

ADD . /opt/estbot

RUN dotnet publish /opt/estbot/src/Automation --configuration Release --runtime linux-x64

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/src/Automation/bin/Release/netcoreapp2.1/linux-x64/publish/Estranged.Automation"]
