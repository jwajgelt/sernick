@ECHO OFF

:: Usage: ./run_in_docker.bat examples/.../xxx.ser

set currentdir=%~dp0

docker rmi sernick-image:latest 2> nul

cd src\sernick
dotnet publish -c Release
cd ..\..
docker build -t sernick-image -f src/sernick/Dockerfile .
docker run -v %currentdir%examples:/sernick/examples --rm sernick-image %1 --execute
