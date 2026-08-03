# Stage 1: base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# Stage 2: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OcelotGateway/OcelotGateway.csproj", "OcelotGateway/"]
RUN dotnet restore "./OcelotGateway/OcelotGateway.csproj"
COPY . .
WORKDIR "/src/OcelotGateway"
RUN dotnet build "./OcelotGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OcelotGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# ocelot.json và ocelot.Production.json đã được copy vào /app/publish bởi dotnet publish
ENTRYPOINT ["dotnet", "OcelotGateway.dll"]
