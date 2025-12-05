# 1. Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# 2. Cài FFmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# 3. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["StreamAudio/StreamAudio.csproj", "StreamAudio/"]
RUN dotnet restore "StreamAudio/StreamAudio.csproj"
COPY . .
WORKDIR "/src/StreamAudio"
RUN dotnet build "StreamAudio.csproj" -c Release -o /app/build

# 4. Publish stage
FROM build AS publish
RUN dotnet publish "StreamAudio.csproj" -c Release -o /app/publish

# 5. Runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StreamAudio.dll"]
