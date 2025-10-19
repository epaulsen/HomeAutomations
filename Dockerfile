# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Install CA certificates for SSL connections
RUN apk add --no-cache ca-certificates

# Copy project file and restore dependencies
COPY src/NetDaemon/HomeAutomations.csproj ./src/NetDaemon/
RUN dotnet restore src/NetDaemon/HomeAutomations.csproj -r linux-musl-x64

# Copy everything else and build
COPY . .
RUN dotnet publish src/NetDaemon/HomeAutomations.csproj -c Release -o /app/publish \
    -r linux-musl-x64 \
    --self-contained true \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS runtime
WORKDIR /app

# Install ICU libraries for globalization support
RUN apk add --no-cache icu-libs

# Copy built application
COPY --from=build /app/publish .

# Create apps directory
RUN mkdir -p /app/apps

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=UTC

# Run the application
ENTRYPOINT ["./HomeAutomations"]
