FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

# Install clang for NativeAOT compilation
RUN apt-get update && apt-get install -y clang zlib1g-dev && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["WhaleWire/WhaleWire.csproj", "WhaleWire/"]
RUN dotnet restore "WhaleWire/WhaleWire.csproj"
COPY . .
WORKDIR "/src/WhaleWire"
RUN dotnet build "./WhaleWire.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WhaleWire.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Use a minimal runtime image for NativeAOT (no .NET runtime needed)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["./WhaleWire"]
