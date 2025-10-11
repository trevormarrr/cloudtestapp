# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ./CloudTestApp/CloudTestApp.csproj
RUN dotnet publish ./CloudTestApp/CloudTestApp.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Heroku provides a PORT env var; bind Kestrel to it (we’ll also handle this in Program.cs)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
CMD ["dotnet", "CloudTestApp.dll"]
