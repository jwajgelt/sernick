FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /sernick

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /sernick
COPY --from=build-env /sernick/out .
RUN apt-get update && apt-get -y install nasm && apt-get -y install gcc
ENTRYPOINT ["dotnet", "sernick.dll"]

