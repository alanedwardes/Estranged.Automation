FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/runtime:3.1

RUN mkdir /opt/estbot

ADD build/linux-x64 /opt/estbot

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
