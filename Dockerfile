# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

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

# Set the entry point for the application
ENTRYPOINT ["dotnet", "service.dll"]