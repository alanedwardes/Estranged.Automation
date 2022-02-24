FROM mcr.microsoft.com/dotnet/runtime:3.1-bullseye-slim-arm64v8

RUN mkdir /opt/estbot

ADD build/linux-x64 /opt/estbot

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
