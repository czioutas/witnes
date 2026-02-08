FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /app

# Copy the project files first for better layer caching
COPY libs/Libs/Libs.csproj ./libs/Libs/
COPY api/Api/Api.csproj ./api/Api/

# Restore dependencies
RUN dotnet restore api/Api/Api.csproj

# Copy the source code
COPY libs/Libs/ ./libs/Libs/
COPY api/Api/ ./api/Api/

# Build and publish
RUN dotnet publish api/Api/Api.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# Install Kerberos libraries required for PostgreSQL GSS encryption
RUN apk add --no-cache krb5-libs
RUN apk add --no-cache curl

WORKDIR /app
EXPOSE 7070

COPY --from=build /app/out ./
# COPY --from=build /app/api/Api/Data/RAW/ ./Data/RAW/
ENTRYPOINT ["dotnet", "api.dll"]