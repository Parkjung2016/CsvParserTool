param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$allowedExtensions = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('.cs', '.csproj', '.props', '.targets', '.slnx', '.xml', '.md', '.ps1', '.yml', '.yaml'),
    [System.StringComparer]::OrdinalIgnoreCase)
$excludedDirectories = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('.git', 'bin', 'obj', 'dist', 'release'),
    [System.StringComparer]::OrdinalIgnoreCase)
$violations = [System.Collections.Generic.List[string]]::new()
$rootPath = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar


Get-ChildItem -LiteralPath $Root -Recurse -File | ForEach-Object {
    $file = $_
    if (-not $allowedExtensions.Contains($file.Extension)) {
        return
    }

    $relative = $file.FullName.Substring($rootPath.Length)
    $segments = $relative -split '[\\/]'
    if ($segments | Where-Object { $excludedDirectories.Contains($_) }) {
        return
    }

    try {
        $lines = [System.IO.File]::ReadAllLines($file.FullName, $strictUtf8)
    }
    catch {
        $violations.Add("$relative : invalid UTF-8")
        return
    }

    for ($index = 0; $index -lt $lines.Length; $index++) {
        $line = $lines[$index]
        if ($line.IndexOf([char]0xFFFD) -ge 0) {
            $violations.Add("$relative`:$($index + 1) : Unicode replacement character")
        }

        if ($line -match '[\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]') {
            $violations.Add("$relative`:$($index + 1) : possible mojibake CJK character")
        }
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Error $_ }
    throw "Text encoding validation failed: $($violations.Count) issue(s)."
}

Write-Host 'Text encoding validation passed (strict UTF-8, no mojibake markers).'
