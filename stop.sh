docker stop $(docker ps -a -q --filter ancestor=aspcoreeasyupload --format="{{.ID}}")
