#!/bin/bash
docker exec -t -i $(docker ps -q --filter ancestor=aspcorehadoopupload --format="{{.ID}}") bash
