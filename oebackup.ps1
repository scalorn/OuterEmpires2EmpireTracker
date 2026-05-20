$env:GIT_SSH_COMMAND="ssh -2 -i C:\\users\\jasegler\\.ssh\\id_scalorn"
git push backup
git push origin
git push niabus
Write-Host "Backup complete."

dotnet publish OE2EmpireTracker.Server -c Release -r linux-x64 --self-contained -o ./publish/linux
md T:\oe2server\
xcopy publish\linux\. T:\oe2server\. /e /h /r /c /y
Write-Host "Publish complete."
