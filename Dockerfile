FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AppCicd/AppCicd.csproj AppCicd/
RUN dotnet restore AppCicd/AppCicd.csproj
COPY AppCicd/ AppCicd/
RUN dotnet publish AppCicd/AppCicd.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AppCicd.dll"]