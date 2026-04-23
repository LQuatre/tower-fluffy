# Deployment script for TowerFluffy Server to Linux
$RemoteHost = "fivem.lquatre.fr"
$RemoteUser = "lquatre"
$RemotePath = "/home/lquatre/tower-fluffy-server"

Write-Host "Publishing TowerFluffy.Server..." -ForegroundColor Cyan
dotnet publish src/Server/TowerFluffy.Server.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false -o ./publish/server

Write-Host "Creating remote directory..." -ForegroundColor Cyan
ssh $RemoteUser@$RemoteHost "mkdir -p $RemotePath"

Write-Host "Copying files to $RemoteHost..." -ForegroundColor Cyan
scp -r ./publish/server/* "$($RemoteUser)@$($RemoteHost):$RemotePath/"

Write-Host "Setting permissions..." -ForegroundColor Cyan
ssh $RemoteUser@$RemoteHost "chmod +x $RemotePath/TowerFluffy.Server"

Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "To run the server, SSH into the machine and execute:" -ForegroundColor Yellow
Write-Host "cd $RemotePath && ./TowerFluffy.Server" -ForegroundColor White
