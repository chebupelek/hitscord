FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["hitscord_new/hitscord_new.csproj", "hitscord_new/"]
COPY ["HitscordLibrary/HitscordLibrary.csproj", "HitscordLibrary/"]
RUN dotnet restore "./hitscord_new/./hitscord_new.csproj"
COPY . .
WORKDIR "/src/hitscord_new"
RUN dotnet build "./hitscord_new.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./hitscord_new.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "hitscord_new.dll"]