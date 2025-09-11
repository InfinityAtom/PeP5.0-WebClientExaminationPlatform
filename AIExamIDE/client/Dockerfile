# Stage 1: Build the Blazor Server app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy proj and restore as distinct layers for caching
COPY *.csproj .
RUN dotnet restore

# Copy remaining source and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Run the app using ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy from build stage
COPY --from=build /app/publish .

# Expose port 5000
EXPOSE 5000

# Configure the ASP.NET Core URL and environment
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AIExamIDE.dll"]