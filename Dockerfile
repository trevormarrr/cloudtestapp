# ==========================
# Build stage
# ==========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ./CloudTestApp/CloudTestApp.csproj
RUN dotnet publish ./CloudTestApp/CloudTestApp.csproj -c Release -o /app/publish

# ==========================
# Runtime stage
# ==========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# ✅ Bind Kestrel to Cloud Run's expected port
ENV ASPNETCORE_URLS=http://+:8080

# ✅ Let Cloud Run know which port to expose
EXPOSE 8080

# ✅ Run the app
ENTRYPOINT ["dotnet", "CloudTestApp.dll"]
