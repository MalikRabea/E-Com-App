# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and csproj files
COPY E-Com.sln ./
COPY E-Com.API/E-Com.API.csproj E-Com.API/
COPY E-Com.Core/E-Com.Core.csproj E-Com.Core/
COPY E-Com.infrastructure/E-Com.infrastructure.csproj E-Com.infrastructure/
RUN dotnet restore E-Com.sln

# Copy all source code
COPY . .

# Build and publish
WORKDIR /src/E-Com.API
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Configure ASP.NET Core to listen on Render's PORT
ENV ASPNETCORE_URLS=http://+:${PORT}

ENTRYPOINT ["dotnet", "E-Com.API.dll"]
