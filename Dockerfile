# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ProjetoCloud.Api/ProjetoCloud.Api.csproj src/ProjetoCloud.Api/
RUN dotnet restore src/ProjetoCloud.Api/ProjetoCloud.Api.csproj

COPY . .
RUN dotnet publish src/ProjetoCloud.Api/ProjetoCloud.Api.csproj -c Release -o /app/publish --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# O Render define PORT em tempo de execução; 8080 é o padrão local
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProjetoCloud.Api.dll"]
