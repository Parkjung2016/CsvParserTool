# Release 빌드 후 배포용 폴더에 단일 exe만 복사합니다.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "bin\Release\net481"
$dist = Join-Path $root "dist"

dotnet build "$root\CSVParserTool.slnx" -c Release

New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item (Join-Path $out "DataToolGUI.exe") (Join-Path $dist "DataToolGUI.exe") -Force
Copy-Item (Join-Path $out "DataTool.exe") (Join-Path $dist "DataTool.exe") -Force

Write-Host ""
Write-Host "배포 폴더: $dist"
Write-Host "  DataToolGUI.exe  (GUI — 이 파일만 복사해도 실행 가능)"
Write-Host "  DataTool.exe     (CLI, 선택)"
