#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/AuthServer.Server/AuthServer.Server.csproj", "src/AuthServer.Server/"]
RUN dotnet restore "src/AuthServer.Server/AuthServer.Server.csproj"
COPY . .
WORKDIR "/src/src/AuthServer.Server"
RUN dotnet build "AuthServer.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthServer.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthServer.Server.dll"]
