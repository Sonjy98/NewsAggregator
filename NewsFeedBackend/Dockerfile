# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# copy the rest of the source (including Migrations, Prompts, etc.)
COPY . ./

# publish the app
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# explicitly include Prompts folder if present
RUN if [ -d "Prompts" ]; then \
      mkdir -p /app/publish/Prompts && \
      cp -r Prompts/* /app/publish/Prompts/; \
    fi

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# copy published output from build stage
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NewsFeedBackend.dll"]
