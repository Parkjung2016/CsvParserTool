$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$src = Join-Path $PSScriptRoot '..\assets\pjdev-icon-source.png'
if (-not (Test-Path $src)) {
    $src = 'C:\Users\mc240\.cursor\projects\e-Git-CsvParserTool\assets\pjdev-icon-source.png'
}

$dest = Join-Path $PSScriptRoot '..\Properties\AppIcon.ico'
$sizes = @(16, 32, 48, 256)

function Get-BitmapBytes([System.Drawing.Bitmap]$bitmap) {
    $rect = New-Object Drawing.Rectangle 0, 0, $bitmap.Width, $bitmap.Height
    $data = $bitmap.LockBits($rect, [Drawing.Imaging.ImageLockMode]::ReadOnly, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $length = $data.Stride * $bitmap.Height
        $buffer = New-Object byte[] $length
        [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $buffer, 0, $length)

        $xor = New-Object byte[] ($bitmap.Width * $bitmap.Height * 4)
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            $srcOffset = $y * $data.Stride
            $dstOffset = ($bitmap.Height - 1 - $y) * $bitmap.Width * 4
            [Array]::Copy($buffer, $srcOffset, $xor, $dstOffset, $bitmap.Width * 4)
        }

        $andMaskRow = [Math]::Ceiling($bitmap.Width / 32.0) * 4
        $andMask = New-Object byte[] ($andMaskRow * $bitmap.Height)
        $headerSize = 40
        $imageSize = $headerSize + $xor.Length + $andMask.Length
        $result = New-Object byte[] $imageSize

        $bufferStream = [IO.MemoryStream]::new([byte[]]$result)
        $bw = [IO.BinaryWriter]::new($bufferStream)
        $bw.Write([int32]40)
        $bw.Write([int32]$bitmap.Width)
        $bw.Write([int32](2 * $bitmap.Height))
        $bw.Write([int16]1)
        $bw.Write([int16]32)
        $bw.Write([int32]0)
        $bw.Write([int32]$imageSize)
        $bw.Write([int32]0)
        $bw.Write([int32]0)
        $bw.Write([int32]0)
        $bw.Write([int32]0)
        $bw.Close()

        [Array]::Copy($xor, 0, $result, $headerSize, $xor.Length)
        [Array]::Copy($andMask, 0, $result, $headerSize + $xor.Length, $andMask.Length)
        return ,$result
    }
    finally {
        $bitmap.UnlockBits($data)
    }
}

$entries = @()
foreach ($size in $sizes) {
    $bmp = New-Object Drawing.Bitmap($src)
    $resized = New-Object Drawing.Bitmap($size, $size, [Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($resized)
    $g.Clear([Drawing.Color]::Transparent)
    $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.DrawImage($bmp, 0, 0, $size, $size)
    $g.Dispose()
    $bmp.Dispose()

    $entries += [PSCustomObject]@{
        Size = $size
        Data = (Get-BitmapBytes $resized)
    }
    $resized.Dispose()
}

$ms = New-Object IO.MemoryStream
$bw = New-Object IO.BinaryWriter($ms)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$entries.Count)

$offset = 6 + (16 * $entries.Count)
foreach ($entry in $entries) {
    $w = [byte]([Math]::Min($entry.Size, 255))
    $h = $w
    if ($entry.Size -ge 256) {
        $w = [byte]0
        $h = [byte]0
    }

    $bw.Write($w)
    $bw.Write($h)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$entry.Data.Length)
    $bw.Write([uint32]$offset)
    $offset += $entry.Data.Length
}

foreach ($entry in $entries) {
    $bw.Write([byte[]]$entry.Data, 0, $entry.Data.Length)
}

$bw.Flush()
[IO.File]::WriteAllBytes($dest, $ms.ToArray())
Write-Host "Created $dest"
