git add *

if [ "$1" ]
then
git commit -a -m "$1"
else
git commit -a -m go
fi

git push origin master
