FROM mcr.microsoft.com/dotnet/core/sdk:3.1

RUN mkdir /opt/estbot

ADD . /opt/estbot

RUN dotnet publish /opt/estbot/src/Automation --configuration Release --runtime linux-x64

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/src/Automation/bin/Release/netcoreapp3.1/linux-x64/publish/Estranged.Automation"]
