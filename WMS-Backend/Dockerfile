FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WMS.Domain/WMS.Domain.csproj WMS.Domain/
COPY WMS.Application/WMS.Application.csproj WMS.Application/
COPY WMS.Infrastructure/WMS.Infrastructure.csproj WMS.Infrastructure/
COPY WMS.API/WMS.API.csproj WMS.API/
COPY WMS.BootstrapAdmin/WMS.BootstrapAdmin.csproj WMS.BootstrapAdmin/
RUN dotnet restore WMS.API/WMS.API.csproj \
    && dotnet restore WMS.BootstrapAdmin/WMS.BootstrapAdmin.csproj

COPY . .
RUN dotnet publish WMS.API/WMS.API.csproj -c Release --no-restore -o /out/api \
    && dotnet publish WMS.BootstrapAdmin/WMS.BootstrapAdmin.csproj -c Release --no-restore -o /out/maintenance

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# The runtime image does not include curl; it is used only by the local liveness probe.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /out/api/ /app/
COPY --from=build /out/maintenance/ /maintenance/
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "WMS.API.dll"]
