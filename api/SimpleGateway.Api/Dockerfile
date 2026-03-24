# Dockerfile for SimpleGateway.Api

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SimpleGateway.Api.csproj", "./"]
RUN dotnet restore "SimpleGateway.Api.csproj"

COPY . .
RUN dotnet publish "SimpleGateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Bind to container ports matching app config
ENV ASPNETCORE_URLS=http://+:8000;http://+:8001
ENV ASPNETCORE_ENVIRONMENT=Docker

EXPOSE 8000
EXPOSE 8001

ENTRYPOINT ["dotnet", "SimpleGateway.Api.dll"]
