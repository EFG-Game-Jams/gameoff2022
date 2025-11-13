#!/bin/bash
REMOTE_NAME=krekhuis2
REMOTE_USER=tijmen
REMOTE_TEMP_DIR="/home/$REMOTE_USER/temp/taintrocket"
REMOTE_SRV_DIR=/srv/taintrocket

dotnet publish src/server/ -o published/

# Fix bug with .NET 10 that publishes unnecessary directories
rm -r published/BuildHost-*

# Remove appsettings as these already exist on the deployment target
rm  published/appsettings*.json
# Ensure we don´t accidentally whack the server database 
rm published/*.db

ssh -t $REMOTE_NAME 'rm -r $REMOTE_TEMP_DIR'
scp -r published $REMOTE_NAME:$REMOTE_TEMP_DIR

ssh -t $REMOTE_NAME 'sudo systemctl stop taintrocket.service; \
sudo rm -r $REMOTE_SRV_DIR/runtimes/; \
sudo rm $REMOTE_SRV_DIR/Game.Server* $REMOTE_SRV_DIR/web.config; \
sudo cp -rv $REMOTE_TEMP_DIR/* $REMOTE_SRV_DIR/ && \
sudo chown -R www-data:www-data $REMOTE_SRV_DIR/* && \
sudo systemctl start taintrocket.service && \
rm -r $REMOTE_TEMP_DIR/'