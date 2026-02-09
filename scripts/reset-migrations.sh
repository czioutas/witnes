#!/bin/bash
set -e

# Stop and remove the docker containers
docker-compose -f code/docker-compose.yaml down -v

# Remove the old migrations
rm -rf code/api/Api/Migrations

# Add a new migration
dotnet ef migrations add Init1 --project code/api/Api --context ApplicationDbContext
