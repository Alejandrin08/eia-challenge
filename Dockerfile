FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Eia.Api/Eia.Api.csproj", "Eia.Api/"]
COPY ["Eia.Data/Eia.Data.csproj", "Eia.Data/"]
COPY ["Eia.Connector/Eia.Connector.csproj", "Eia.Connector/"]

RUN dotnet restore "Eia.Api/Eia.Api.csproj"
RUN dotnet restore "Eia.Connector/Eia.Connector.csproj"

COPY . .

RUN dotnet publish "Eia.Connector/Eia.Connector.csproj" -c Release -o /app/connector
RUN dotnet publish "Eia.Api/Eia.Api.csproj"             -c Release -o /app/api

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/connector ./connector
COPY --from=build /app/api .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

RUN mkdir -p /app/data /app/connector/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "Eia.Api.dll"]