# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8000
EXPOSE 8001

# Ensure ASP.NET Core listens on the ports Kestrel is configured for.
# HTTP on 8000 and HTTPS on 8001 (HTTPS will be ignored inside the container per Program.cs logic).
ENV ASPNETCORE_URLS="http://+:8000;https://+:8001"


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SimpleGateway.Api/SimpleGateway.Api.csproj", "SimpleGateway.Api/"]
RUN dotnet restore "./SimpleGateway.Api/SimpleGateway.Api.csproj"
COPY . .
WORKDIR "/src/SimpleGateway.Api"
RUN dotnet build "./SimpleGateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SimpleGateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleGateway.Api.dll"]