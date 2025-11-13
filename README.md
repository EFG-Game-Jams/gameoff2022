# gameoff2022

Game Off '22 jam contribution by members of EFG

## First Time Setup

Ensure you have Git LFS setup by running the following command

```bash
git lfs install
```

If you are using GitHub for desktop or another Git GUI client then this may be
done automatically for you.

## Server deployment

The Taintrocket leaderboard server is run as a service by using a systemd unit file (`taintrocket.service`). This could be, for example:

```
[Unit]
Description=A .NET Core application providing the Taintrocket Leaderboard REST API
Requires=network.target
After=network.target

[Service]
Type=simple
Environment="DOTNET_ROOT=/usr/local/dotnet"
WorkingDirectory=/srv/taintrocket/
ExecStart=/usr/local/dotnet/dotnet Game.Server.dll --urls "http://192.168.2.10:5001"
Restart=always
RestartSec=30
User=www-data

[Install]
WantedBy=multi-user.target
```

For convenience a build and deploy script `build-and-deploy-server.sh` can be found in the repository root.
