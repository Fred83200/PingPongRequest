# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Install curl (if needed for other purposes)
RUN apt-get update && apt-get install -y curl

# Copy csproj files and restore as distinct layers
COPY src/service.csproj ./src/
COPY tests/service-test.csproj ./tests/
RUN dotnet restore src/service.csproj

# Copy everything else and build the app
COPY . .
RUN dotnet publish src/service.csproj -c Release -o out

# Use the official ASP.NET Core image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .

# Expose port 80
EXPOSE 80

# Set environment variables (if needed)
ENV CosmosDb__EndpointUri=https://cosmosdb-emulator:8081
ENV CosmosDb__PrimaryKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==

# Set the entry point for the application
ENTRYPOINT ["dotnet", "service.dll"]
