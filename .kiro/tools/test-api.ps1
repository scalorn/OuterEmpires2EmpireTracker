$token = "X3Qs9l4R6ZyM0OYUYWJ_4uaYnGuQSufSF2k_x5ho6DY"
$base = "https://192.168.4.36:5443"
$headers = @{ "Authorization" = "Bearer $token" }

# Force TLS 1.2 and ignore cert errors
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

Write-Host "=== GET / ==="
try {
    $r = Invoke-WebRequest -Uri "$base/" -UseBasicParsing
    Write-Host "Status: $($r.StatusCode)"
    Write-Host $r.Content.Substring(0, [Math]::Min(500, $r.Content.Length))
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) { Write-Host "Status: $($_.Exception.Response.StatusCode.value__)" }
}

Write-Host ""
Write-Host "=== GET /api/v1/characters ==="
try {
    $r = Invoke-WebRequest -Uri "$base/api/v1/characters" -Headers $headers -UseBasicParsing
    Write-Host "Status: $($r.StatusCode)"
    Write-Host $r.Content.Substring(0, [Math]::Min(800, $r.Content.Length))
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        Write-Host "Body: $($reader.ReadToEnd())"
        Write-Host "Status: $($_.Exception.Response.StatusCode.value__)"
    }
}

Write-Host ""
Write-Host "=== GET /api/v1/sync ==="
try {
    $r = Invoke-WebRequest -Uri "$base/api/v1/sync" -Headers $headers -UseBasicParsing
    Write-Host "Status: $($r.StatusCode)"
    Write-Host $r.Content.Substring(0, [Math]::Min(800, $r.Content.Length))
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        Write-Host "Body: $($reader.ReadToEnd())"
        Write-Host "Status: $($_.Exception.Response.StatusCode.value__)"
    }
}

Write-Host ""
Write-Host "=== GET /config.json ==="
try {
    $r = Invoke-WebRequest -Uri "$base/config.json" -UseBasicParsing
    Write-Host "Status: $($r.StatusCode)"
    Write-Host $r.Content
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) { Write-Host "Status: $($_.Exception.Response.StatusCode.value__)" }
}
