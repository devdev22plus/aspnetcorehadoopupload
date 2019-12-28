FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
ENV IS_DOCKER_ENV=true
WORKDIR /app
COPY . .




CMD ASPNETCORE_URLS=http://*:$PORT dotnet aspcorehadoopupload.dll > dotnet.log