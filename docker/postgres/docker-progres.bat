@echo off

REM Define environment variables
set POSTGRES_PASSWORD=Shcdlhgm1!
set PGADMIN_DEFAULT_EMAIL=fchy@outlook.com
set PGADMIN_DEFAULT_PASSWORD=Shcldhgm1!
set NETWORK_NAME=hmm-network
set POSTGRES_CONTAINER_NAME=hmm-postgres
set PGADMIN_CONTAINER_NAME=hmm-pgadmin

REM Create a Docker network (if it doesn't already exist)
docker network ls | findstr /C:%NETWORK_NAME% || docker network create %NETWORK_NAME%

REM Pull the latest PostgreSQL and pgAdmin images
docker pull postgres
docker pull dpage/pgadmin4:latest

REM Run the PostgreSQL container
docker run --name %POSTGRES_CONTAINER_NAME% -e POSTGRES_PASSWORD=%POSTGRES_PASSWORD% ^
  -p 5432:5432 --network %NETWORK_NAME% ^
  -v G:/Projects2/Hmm/data/progres:/var/lib/postgresql/data  -d postgres

REM Run the pgAdmin container
docker run --name %PGADMIN_CONTAINER_NAME% -p 8080:80 ^
  -e PGADMIN_DEFAULT_EMAIL=%PGADMIN_DEFAULT_EMAIL% ^
  -e PGADMIN_DEFAULT_PASSWORD=%PGADMIN_DEFAULT_PASSWORD% ^
  --network %NETWORK_NAME% -d dpage/pgadmin4

REM Copy and execute the SQL script
docker cp init-db.sql %POSTGRES_CONTAINER_NAME%:/init-db.sql
docker exec -u postgres %POSTGRES_CONTAINER_NAME% psql -d postgres -f /init-db.sql


echo PostgreSQL and pgAdmin setup completed.
echo Access pgAdmin at http://localhost:8080
