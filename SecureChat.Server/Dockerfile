# Используем базовый образ ASP.NET Core для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

# Используем SDK-образ для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SecureChat.Server/SecureChat.Server.csproj", "SecureChat.Server/"]
RUN dotnet restore "SecureChat.Server/SecureChat.Server.csproj"
COPY . .
WORKDIR "/src/SecureChat.Server"
RUN dotnet build "SecureChat.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикация
FROM build AS publish
RUN dotnet publish "SecureChat.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecureChat.Server.dll"]
