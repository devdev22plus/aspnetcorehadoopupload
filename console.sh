#!/bin/bash
docker exec -t -i $(docker ps -q --filter ancestor=aspcoreeasyupload --format="{{.ID}}") bash
