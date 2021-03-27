FROM ubuntu:latest

RUN mkdir /opt/estbot

ADD build/linux-x64 /opt/estbot

VOLUME ["/data"]

ENTRYPOINT ["/opt/estbot/Estranged.Automation"]
