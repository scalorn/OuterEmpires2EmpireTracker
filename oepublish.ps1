dotnet publish OE2EmpireTracker.Server -c Release -r linux-x64 --self-contained -o ./publish/linux
md T:\oe2server\
xcopy publish\linux\. T:\oe2server\. /e /h /r /c /y
Write-Host "Publish complete."
