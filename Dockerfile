# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

# Use build arguments for multi-platform support
ARG TARGETARCH
WORKDIR /src

# Install CA certificates for SSL connections
RUN apk add --no-cache ca-certificates

# Copy project file and restore dependencies
COPY src/NetDaemon/HomeAutomations.csproj ./src/NetDaemon/

# Map Docker's TARGETARCH to .NET runtime identifiers
# amd64 -> linux-musl-x64, arm64 -> linux-musl-arm64, arm -> linux-musl-arm
RUN if [ "$TARGETARCH" = "amd64" ]; then \
        dotnet restore src/NetDaemon/HomeAutomations.csproj -r linux-musl-x64; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
        dotnet restore src/NetDaemon/HomeAutomations.csproj -r linux-musl-arm64; \
    elif [ "$TARGETARCH" = "arm" ]; then \
        dotnet restore src/NetDaemon/HomeAutomations.csproj -r linux-musl-arm; \
    else \
        echo "Unsupported architecture: $TARGETARCH" && exit 1; \
    fi

# Copy everything else and build
COPY . .

# Publish for the appropriate architecture
RUN if [ "$TARGETARCH" = "amd64" ]; then \
        dotnet publish src/NetDaemon/HomeAutomations.csproj -c Release -o /app/publish \
            -r linux-musl-x64 \
            --self-contained true \
            /p:PublishTrimmed=false \
            /p:PublishSingleFile=false; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
        dotnet publish src/NetDaemon/HomeAutomations.csproj -c Release -o /app/publish \
            -r linux-musl-arm64 \
            --self-contained true \
            /p:PublishTrimmed=false \
            /p:PublishSingleFile=false; \
    elif [ "$TARGETARCH" = "arm" ]; then \
        dotnet publish src/NetDaemon/HomeAutomations.csproj -c Release -o /app/publish \
            -r linux-musl-arm \
            --self-contained true \
            /p:PublishTrimmed=false \
            /p:PublishSingleFile=false; \
    else \
        echo "Unsupported architecture: $TARGETARCH" && exit 1; \
    fi

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
