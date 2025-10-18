# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY src/NetDaemon/HomeAutomations.csproj ./src/NetDaemon/
RUN dotnet restore src/NetDaemon/HomeAutomations.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish src/NetDaemon/HomeAutomations.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
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
