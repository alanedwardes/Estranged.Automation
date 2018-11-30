FROM microsoft/dotnet:2.1-sdk

RUN mkdir /opt/estbot

ADD . /opt/estbot

RUN cd /opt/estbot

RUN dotnet publish --configuration Release --runtime linux-x64

ENTRYPOINT ["/opt/estbot/src/Automation/Release/netcoreapp2.0/publish/Estranged.Automation"]
