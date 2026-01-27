# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/Hmm.ServiceApi/Hmm.ServiceApi.csproj", "src/Hmm.ServiceApi/"]
COPY ["src/Hmm.ServiceApi.DtoEntity/Hmm.ServiceApi.DtoEntity.csproj", "src/Hmm.ServiceApi.DtoEntity/"]
COPY ["src/Hmm.Automobile/Hmm.Automobile.csproj", "src/Hmm.Automobile/"]
COPY ["src/Hmm.Core/Hmm.Core.csproj", "src/Hmm.Core/"]
COPY ["src/Hmm.Core.Map/Hmm.Core.Map.csproj", "src/Hmm.Core.Map/"]
COPY ["src/Hmm.Utility/Hmm.Utility.csproj", "src/Hmm.Utility/"]
COPY ["src/Hmm.Core.Dal.EF/Hmm.Core.Dal.EF.csproj", "src/Hmm.Core.Dal.EF/"]
COPY ["src/Hmm.Infrastructure/Hmm.Infrastructure.csproj", "src/Hmm.Infrastructure/"]
COPY ["src/Hmm.Contract/Hmm.Contract.csproj", "src/Hmm.Contract/"]

RUN dotnet restore "src/Hmm.ServiceApi/Hmm.ServiceApi.csproj"

# Copy source code
COPY . .

WORKDIR "src/Hmm.ServiceApi"
RUN dotnet build "Hmm.ServiceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hmm.ServiceApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hmm.ServiceApi.dll"]
