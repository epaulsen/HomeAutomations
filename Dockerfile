# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY HomeAutomations.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy built application
COPY --from=build /app/publish .

# Create apps directory
RUN mkdir -p /app/apps

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=UTC

# Run the application
ENTRYPOINT ["dotnet", "HomeAutomations.dll"]
