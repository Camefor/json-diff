FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["JsonDiffPlatform.Api.csproj", "."]
RUN dotnet restore "JsonDiffPlatform.Api.csproj"
COPY . .
RUN dotnet publish "JsonDiffPlatform.Api.csproj" -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/data
ENTRYPOINT ["dotnet", "JsonDiffPlatform.Api.dll"]
