@echo off

REM Define environment variables
set SA_PASSWORD=Shcdlhgm1!
set NETWORK_NAME=hmm-network
set SQLSERVER_CONTAINER_NAME=hmm-sqlserver
set SEQ_CONTAINER_NAME=hmm-seq

REM Create a Docker network (if it doesn't already exist)
docker network ls | findstr /C:%NETWORK_NAME% || docker network create %NETWORK_NAME%

REM Pull the latest SQL Server and Seq images
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull datalust/seq:latest

REM Run the SQL Server container
docker run --name %SQLSERVER_CONTAINER_NAME% ^
  -e "ACCEPT_EULA=Y" ^
  -e "MSSQL_SA_PASSWORD=%SA_PASSWORD%" ^
  -e "MSSQL_PID=Developer" ^
  -p 1433:1433 ^
  --network %NETWORK_NAME% ^
  -v G:/Projects2/Hmm/data/sqlserver:/var/opt/mssql/data ^
  -d mcr.microsoft.com/mssql/server:2022-latest

REM Wait for SQL Server to start
echo Waiting for SQL Server to start...
timeout /t 30 /nobreak

REM Run the Seq container for logging
docker run --name %SEQ_CONTAINER_NAME% ^
  -e "ACCEPT_EULA=Y" ^
  -p 5341:5341 ^
  -p 8081:80 ^
  --network %NETWORK_NAME% ^
  -d datalust/seq:latest

REM Copy and execute the SQL script
docker cp init-db.sql %SQLSERVER_CONTAINER_NAME%:/init-db.sql
docker exec %SQLSERVER_CONTAINER_NAME% /opt/mssql-tools18/bin/sqlcmd ^
  -S localhost -U sa -P "%SA_PASSWORD%" -C -i /init-db.sql

echo.
echo SQL Server setup completed.
echo Connection string: Server=localhost,1433;Database=hmm;User Id=sa;Password=%SA_PASSWORD%;TrustServerCertificate=True
echo Access Seq at http://localhost:8081
