# Unter https://aka.ms/customizecontainer erfahren Sie, wie Sie Ihren Debugcontainer anpassen und wie Visual Studio dieses Dockerfile verwendet, um Ihre Images für ein schnelleres Debuggen zu erstellen.

# Diese Stufe wird verwendet, wenn sie von VS im Schnellmodus ausgeführt wird (Standardeinstellung für Debugkonfiguration).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Diese Stufe wird zum Erstellen des Dienstprojekts verwendet.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
USER root
RUN apt-get -y update && apt -y install ocrmypdf
RUN apt-get -y install tesseract-ocr-deu
RUN apt-get -y install tesseract-ocr-all
USER app
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/OCRmyPDF-Server/OCRmyPDF-Server.csproj", "src/OCRmyPDF-Server/"]
RUN dotnet restore "./src/OCRmyPDF-Server/OCRmyPDF-Server.csproj"
COPY . .
WORKDIR "/src/src/OCRmyPDF-Server"
RUN dotnet build "./OCRmyPDF-Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Diese Stufe wird verwendet, um das Dienstprojekt zu veröffentlichen, das in die letzte Phase kopiert werden soll.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OCRmyPDF-Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Diese Stufe wird in der Produktion oder bei Ausführung von VS im regulären Modus verwendet (Standard, wenn die Debugkonfiguration nicht verwendet wird).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OCRmyPDF-Server.dll"]