$dotnetDir = "$env:LocalAppData\Microsoft\dotnet"
if (Test-Path "$dotnetDir\dotnet.exe") {
    $env:PATH = "$dotnetDir;$env:PATH"
}
$exePath = Join-Path $PSScriptRoot "bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\WeatherApp.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "Dang bien dich ung dung WinUI 3..." -ForegroundColor Yellow
    & "$dotnetDir\dotnet.exe" build -p:Platform=x64
}
Write-Host "Khoi chay Ung Dung Thoi Tiet WinUI 3..." -ForegroundColor Green
Start-Process $exePath
