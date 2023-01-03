FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /sernick

COPY ./src/sernick ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /sernick
COPY --from=build-env /sernick/out .
COPY ./e2e /e2e
WORKDIR /e2e
RUN apt-get update && apt-get -y install nasm && apt-get -y install gcc && apt-get -y install python3
ENTRYPOINT ["python3", "tester.py", "--compiler", "../sernick/sernick.dll"]
