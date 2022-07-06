#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Hmm.ServiceApi/Hmm.ServiceApi.csproj", "src/Hmm.ServiceApi/"]
COPY ["src/Hmm.ServiceApi.DtoEntity/Hmm.ServiceApi.DtoEntity.csproj", "src/Hmm.ServiceApi.DtoEntity/"]
COPY ["src/Hmm.Automobile/Hmm.Automobile.csproj", "src/Hmm.Automobile/"]
COPY ["src/Hmm.Core/Hmm.Core.csproj", "src/Hmm.Core/"]
COPY ["src/Hmm.Utility/Hmm.Utility.csproj", "src/Hmm.Utility/"]
COPY ["src/Hmm.Core.Dal.EF/Hmm.Core.Dal.EF.csproj", "src/Hmm.Core.Dal.EF/"]
COPY ["src/Hmm.Infrastructure/Hmm.Infrastructure.csproj", "src/Hmm.Infrastructure/"]
COPY ["src/Hmm.Contract/Hmm.Contract.csproj", "src/Hmm.Contract/"]
RUN dotnet restore "src/Hmm.ServiceApi/Hmm.ServiceApi.csproj"
COPY . .
WORKDIR "/src/src/Hmm.ServiceApi"
RUN dotnet build "Hmm.ServiceApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hmm.ServiceApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hmm.ServiceApi.dll"]