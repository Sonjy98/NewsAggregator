# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# context is already NewsFeedBackend/, so copy patterns are local
COPY *.csproj ./
RUN dotnet restore

# now copy the rest (incl. Migrations)
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","NewsFeedBackend.dll"]
