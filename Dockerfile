FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
RUN mkdir -p /data/assets/avatars /data/assets/banners
WORKDIR /src/Resonance
CMD ["dotnet", "watch", "--non-interactive", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Resonance/Resonance.csproj Resonance/
COPY Resonance.Core/Resonance.Core.csproj Resonance.Core/
COPY Resonance.Database/Resonance.Database.csproj Resonance.Database/
COPY Resonance.Server/Resonance.Server.csproj Resonance.Server/
RUN dotnet restore Resonance/Resonance.csproj
COPY Resonance/ Resonance/
COPY Resonance.Core/ Resonance.Core/
COPY Resonance.Database/ Resonance.Database/
COPY Resonance.Server/ Resonance.Server/
RUN dotnet publish Resonance/Resonance.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /data/assets/avatars /data/assets/banners && chown -R $APP_UID:$APP_UID /data/assets
USER $APP_UID
ENTRYPOINT ["dotnet", "Resonance.dll"]
