# Backend: ASP.NET Core + JDK.
#
# Der Build-Kontext ist das REPOSITORY-WURZELVERZEICHNIS, nicht dieser Ordner -
# gebraucht werden Directory.Build.props, Directory.Packages.props und lib/.
# Aufruf deshalb über docker-compose.yml (context: .), nicht mit "docker build"
# aus diesem Verzeichnis heraus.

# ---------------------------------------------------------------- Bauen -------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /quelle

# Erst nur die Projektdateien: solange sich keine Abhängigkeit ändert, bleibt
# die wiederhergestellte Paketschicht im Cache und ein Codeänderung kostet
# keinen erneuten Restore.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/SoopWorkshop.Shared/SoopWorkshop.Shared.csproj                             src/SoopWorkshop.Shared/
COPY src/SoopWorkshop.Backend.Domain/SoopWorkshop.Backend.Domain.csproj             src/SoopWorkshop.Backend.Domain/
COPY src/SoopWorkshop.Backend.Application/SoopWorkshop.Backend.Application.csproj   src/SoopWorkshop.Backend.Application/
COPY src/SoopWorkshop.Backend.Infrastructure/SoopWorkshop.Backend.Infrastructure.csproj src/SoopWorkshop.Backend.Infrastructure/
COPY src/SoopWorkshop.Backend.API/SoopWorkshop.Backend.API.csproj                   src/SoopWorkshop.Backend.API/

RUN dotnet restore src/SoopWorkshop.Backend.API/SoopWorkshop.Backend.API.csproj

# Jetzt der Rest. lib/ muss mit: die .csproj kopiert das JUnit-JAR von dort ins
# Ausgabeverzeichnis, und ohne die Datei bricht der Build.
COPY lib/ lib/
COPY src/ src/

RUN dotnet publish src/SoopWorkshop.Backend.API/SoopWorkshop.Backend.API.csproj \
    -c Release -o /anwendung --no-restore

# --------------------------------------------------------------- Laufen -------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Das JDK gehört in die LAUFZEIT, nicht in den Build: javac und java werden zur
# Auswertung jeder Abgabe als Prozess aufgerufen (IProcessRunner). Fehlen sie,
# scheitert nicht der Start, sondern still jede einzelne Abgabe.
#
# curl für den Healthcheck aus docker-compose.yml - die aspnet-Images bringen
# es nicht mit.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openjdk-21-jdk-headless \
        curl \
        ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /anwendung
COPY --from=build /anwendung .

# Nur http, und nur im Container-Netz. Die TLS-Terminierung und die Umleitung
# von http macht der Reverse Proxy im Frontend-Container.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Das Verzeichnis der DataProtection-Schlüssel muss SCHON IM IMAGE dem
# Benutzer app gehören, unter dem der Prozess läuft.
#
# Grund: docker-compose.yml mountet dort ein benanntes Volume. Ein frisches
# Volume übernimmt Inhalt UND Eigentümer aus dem Image - existiert das
# Verzeichnis dort nicht, legt Docker es an und es gehört root. Der Prozess
# kann dann keinen Schlüssel schreiben, und die ANMELDUNG scheitert mit einem
# 500er, während alles andere läuft. Genau so ist es beim ersten Anlauf
# passiert (UnauthorizedAccessException in FileSystemXmlRepository).
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app

# Nicht als root. Das Arbeitsverzeichnis der Auswertung wird unterhalb von
# /tmp angelegt, darauf hat app Schreibrecht.
USER app

ENTRYPOINT ["dotnet", "SoopWorkshop.Backend.API.dll"]
