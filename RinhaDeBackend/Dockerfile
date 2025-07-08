# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RinhaDeBackend/RinhaDeBackend.csproj", "RinhaDeBackend/"]
COPY ["RinhaDeBackend.ServiceDefaults/RinhaDeBackend.ServiceDefaults.csproj", "RinhaDeBackend.ServiceDefaults/"]
RUN dotnet restore "./RinhaDeBackend/RinhaDeBackend.csproj"
COPY . .
WORKDIR "/src/RinhaDeBackend"
RUN dotnet build "./RinhaDeBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RinhaDeBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaDeBackend.dll"]