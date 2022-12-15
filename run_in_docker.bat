@ECHO OFF
docker rmi sernick-image:latest 2> nul
docker stop sernick 2> nul
docker rm sernick 2> nul

cd src\sernick
dotnet publish -c Release
docker build -t sernick-image -f Dockerfile .
docker create --name sernick sernick-image
docker start sernick
docker wait sernick
echo:
docker logs sernick
