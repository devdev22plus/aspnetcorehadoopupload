#!/bin/bash


docker rmi aspcorehadoopupload
docker rmi $(docker images -qf "dangling=true")
docker rmi $(docker images | grep "aspcorehadoopupload")



#build dotnet
dotnet publish -c Release



#docker
docker build -t aspcorehadoopupload -f Dockerfile ./bin/Release/netcoreapp2.1/publish
