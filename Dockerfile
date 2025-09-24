FROM mcr.microsoft.com/dotnet/runtime:9.0

ADD build/output /opt/estbot

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
