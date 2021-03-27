FROM ubuntu:latest

RUN mkdir /opt/estbot

ADD build/linux-x64 /opt/estbot

VOLUME ["/data"]

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
