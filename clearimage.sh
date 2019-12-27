#!/bin/bash

docker rmi -f aspcoreeasyupload
docker rmi -f $(docker images -qf "dangling=true")

if [ $1 ] && [ $1 == 'all' ]
then
	docker rmi -f $(docker images)
fi
