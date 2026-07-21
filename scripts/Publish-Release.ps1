# GUI/CLI를 각각 단일 실행 파일로 빌드하고 Tool.zip 하나로 묶습니다.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "bin\Release\net481"
$package = Join-Path $root "dist\package"
$zip = Join-Path $root "dist\Tool.zip"

dotnet build (Join-Path $root "CSVParserTool.slnx") -c Release

if (Test-Path -LiteralPath $package) {
    Remove-Item -LiteralPath $package -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $package | Out-Null
Copy-Item (Join-Path $out "DataToolGUI.exe") (Join-Path $package "DataToolGUI.exe") -Force
Copy-Item (Join-Path $out "DataTool.exe") (Join-Path $package "DataTool.exe") -Force
$noticeFiles = @("LICENSE", "ASSET_LICENSE.md", "ASSET_PROVENANCE.md", "THIRD-PARTY-NOTICES.md")
foreach ($noticeFile in $noticeFiles) {
    Copy-Item (Join-Path $root $noticeFile) (Join-Path $package $noticeFile) -Force
}
Copy-Item (Join-Path $root "licenses") (Join-Path $package "licenses") -Recurse -Force
Compress-Archive -Path (Join-Path $package "*") -DestinationPath $zip -Force
Remove-Item -LiteralPath $package -Recurse -Force

Write-Host "배포 파일: $zip"
Write-Host "  DataToolGUI.exe  (GUI 단일 실행 파일)"
Write-Host "  DataTool.exe     (CLI 단일 실행 파일)"
Write-Host "  라이선스 및 서드파티 고지 문서"