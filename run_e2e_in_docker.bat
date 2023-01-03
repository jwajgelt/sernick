@ECHO OFF

docker rmi sernick-e2e-image:latest 2> nul

cd src\sernick
dotnet publish -c Release
cd ..\..
docker build -t sernick-e2e-image -f e2e/Dockerfile .
docker run --rm sernick-e2e-image
