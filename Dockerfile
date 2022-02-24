FROM --platform=linux/arm/v7 mcr.microsoft.com/dotnet/runtime:3.1

ADD build/linux-x64 /opt/estbot

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
