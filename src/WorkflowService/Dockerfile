FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WorkflowService/WorkflowService.csproj WorkflowService/
COPY Shared/Shared.csproj Shared/
RUN dotnet restore WorkflowService/WorkflowService.csproj

COPY . .
RUN dotnet publish WorkflowService/WorkflowService.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "WorkflowService.dll"]
