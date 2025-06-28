# 1. Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# 2. SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["vocab-automation-dotnet.csproj", "./"]
RUN dotnet restore "vocab-automation-dotnet.csproj"

# Copy the rest of the source code
COPY . .
RUN dotnet build "vocab-automation-dotnet.csproj" -c Release -o /app/build

# 3. Publish the app
FROM build AS publish
RUN dotnet publish "vocab-automation-dotnet.csproj" -c Release -o /app/publish

# 4. Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "vocab-automation-dotnet.dll"]
