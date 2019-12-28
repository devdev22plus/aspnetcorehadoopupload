docker stop $(docker ps -a -q --filter ancestor=aspcorehadoopupload --format="{{.ID}}")
