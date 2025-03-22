# Используем базовый образ ASP.NET Core для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Используем SDK-образ для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SecureChat.Client/SecureChat.Client.csproj", "SecureChat.Client/"]
RUN dotnet restore "SecureChat.Client/SecureChat.Client.csproj"
COPY . .
WORKDIR "/src/SecureChat.Client"
RUN dotnet build "SecureChat.Client.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикация
FROM build AS publish
RUN dotnet publish "SecureChat.Client.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SecureChat.Client.dll"]
