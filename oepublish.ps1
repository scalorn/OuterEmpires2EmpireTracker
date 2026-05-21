# 1. Build the frontend (outputs to OE2EmpireTracker.Server/wwwroot/)
cd OE2EmpireTracker.Web
npm run build

# 2. Publish the server as a self-contained Linux binary
cd ..
dotnet publish OE2EmpireTracker.Server/OE2EmpireTracker.Server.csproj -c Release -r linux-x64 --self-contained true -o ./publish/linux

# md T:\oe2server\
# xcopy publish\linux\. T:\oe2server\. /e /h /r /c /y
# Write-Host "Publish complete."
