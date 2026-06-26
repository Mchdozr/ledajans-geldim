FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Ledajans.sln ./
COPY global.json ./
COPY src/Ledajans.Shared/Ledajans.Shared.csproj src/Ledajans.Shared/
COPY src/Ledajans.Client/Ledajans.Client.csproj src/Ledajans.Client/
COPY src/Ledajans.Server/Ledajans.Server.csproj src/Ledajans.Server/

RUN dotnet restore src/Ledajans.Server/Ledajans.Server.csproj

COPY src/ src/
RUN dotnet workload install wasm-tools --skip-manifest-update
RUN dotnet publish src/Ledajans.Server/Ledajans.Server.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Ledajans.Server.dll"]
