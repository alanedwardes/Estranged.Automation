FROM microsoft/dotnet:2.1-sdk

ARG publish_root = /opt/estbot

RUN mkdir $publish_root

ADD . $publish_root

RUN dotnet publish /opt/estbot/src/Automation --configuration Release --runtime linux-x64

ARG executable = $publish_root/src/Automation/bin/Release/netcoreapp2.1/linux-x64/publish/Estranged.Automation

RUN stat $executable

ENTRYPOINT [$executable]
