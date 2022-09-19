FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/AuthServer.DemoApp/AuthServer.DemoApp.csproj", "AuthServer.DemoApp/"]
RUN dotnet restore "src/AuthServer.DemoApp/AuthServer.DemoApp.csproj"
COPY . .
WORKDIR "/src/AuthServer.DemoApp"
RUN dotnet build "AuthServer.DemoApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AuthServer.DemoApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthServer.DemoApp.dll"]
