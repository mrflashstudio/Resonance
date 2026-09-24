FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /src/Resonance
CMD ["dotnet", "watch", "--non-interactive", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Resonance/Resonance.csproj Resonance/
RUN dotnet restore Resonance/Resonance.csproj
COPY Resonance/ Resonance/
RUN dotnet publish Resonance/Resonance.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Resonance.dll"]
