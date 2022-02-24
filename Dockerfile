FROM mcr.microsoft.com/dotnet/runtime:3.1

ADD build/output /opt/estbot

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
