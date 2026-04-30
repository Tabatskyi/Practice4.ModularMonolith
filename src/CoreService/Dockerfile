FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CoreService/Api/Api.csproj CoreService/Api/
COPY CoreService/Modules.Core.Application/Modules.Core.Application.csproj CoreService/Modules.Core.Application/
COPY CoreService/Modules.Core.Infrastructure/Modules.Core.Infrastructure.csproj CoreService/Modules.Core.Infrastructure/
COPY CoreService/Modules.Core.Domain/Modules.Core.Domain.csproj CoreService/Modules.Core.Domain/
COPY Shared/Shared.csproj Shared/
RUN dotnet restore CoreService/Api/Api.csproj

COPY . .
RUN dotnet publish CoreService/Api/Api.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends curl \
	&& rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
