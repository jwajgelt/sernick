@ECHO OFF

:: Usage: ./run_in_docker.bat examples/.../xxx.ser

docker rmi sernick-image:latest 2> nul
docker stop sernick 2> nul
docker rm sernick 2> nul

cd src\sernick
dotnet publish -c Release
cd ..\..
docker build -t sernick-image -f src/sernick/Dockerfile .
docker create --name sernick sernick-image %1 --execute
docker start sernick
docker wait sernick
echo:
docker logs sernick
