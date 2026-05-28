try {
    $asm = [System.Reflection.Assembly]::LoadFrom("d:\projects\OuterEmpires2\OE2EmpireTracker\OE2EmpireTracker.Tests\bin\Debug\OE2EmpireTracker.Tests.dll")
    $type = $asm.GetType("OE2EmpireTracker.Tests.Services.BlueprintLinkageServiceTests")
    if ($type) {
        Write-Host "Type found: $($type.FullName)"
        try {
            $instance = [Activator]::CreateInstance($type)
            Write-Host "Instance created successfully"
        } catch {
            Write-Host "INSTANTIATION ERROR: $($_.Exception.GetType().Name): $($_.Exception.Message)"
            if ($_.Exception.InnerException) {
                Write-Host "INNER: $($_.Exception.InnerException.GetType().Name): $($_.Exception.InnerException.Message)"
                if ($_.Exception.InnerException.InnerException) {
                    Write-Host "INNER2: $($_.Exception.InnerException.InnerException.GetType().Name): $($_.Exception.InnerException.InnerException.Message)"
                }
            }
        }
    } else {
        Write-Host "Type NOT found in assembly"
        $allTypes = $asm.GetTypes()
        Write-Host "Total types in assembly: $($allTypes.Count)"
        $serviceTypes = $allTypes | Where-Object { $_.Namespace -eq "OE2EmpireTracker.Tests.Services" }
        Write-Host "Types in Services namespace: $($serviceTypes.Count)"
        $serviceTypes | ForEach-Object { Write-Host "  $_" }
    }
} catch [System.Reflection.ReflectionTypeLoadException] {
    Write-Host "ReflectionTypeLoadException!"
    $_.Exception.LoaderExceptions | ForEach-Object { Write-Host "  LOADER: $($_.Message)" }
} catch {
    Write-Host "ERROR: $($_.Exception.GetType().Name): $($_.Exception.Message)"
}
