# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the csproj and restore as distinct layers
COPY PingListener.csproj ./
RUN dotnet restore

# Copy the entire project and build it
COPY . ./
RUN dotnet publish -c Release -o out

# Use the ASP.NET Core runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build-env /app/out .

# Expose the port
EXPOSE 80

# Set the entry point for the application
ENTRYPOINT ["dotnet", "PingListener.dll"]
