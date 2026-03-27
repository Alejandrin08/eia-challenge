FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Eia.Api/Eia.Api.csproj", "Eia.Api/"]
COPY ["Eia.Data/Eia.Data.csproj", "Eia.Data/"]
COPY ["Eia.Connector/Eia.Connector.csproj", "Eia.Connector/"]
RUN dotnet restore "Eia.Api/Eia.Api.csproj"
RUN dotnet restore "Eia.Connector/Eia.Connector.csproj"

COPY . .

WORKDIR /src/Eia.Connector
RUN dotnet publish -c Release -o /app/connector

WORKDIR /src/Eia.Api
RUN dotnet publish -c Release -o /app/api

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/connector ./connector
COPY --from=build /app/api .

RUN mkdir -p /app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "Eia.Api.dll"]