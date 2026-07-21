param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]*$')]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$DisplayName,

    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]*$')]
    [string]$GameId
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$gamesDirectory = Join-Path $root "MiniGames\Games"
$className = $Name + "MiniGame"
$filePath = Join-Path $gamesDirectory ($className + ".cs")

if ([string]::IsNullOrWhiteSpace($GameId)) {
    $GameId = "custom." + $Name.ToLowerInvariant()
}

$escapedDisplayName = $DisplayName.Replace('\', '\\').Replace('"', '\"')

if (Test-Path -LiteralPath $filePath) {
    throw "이미 존재하는 게임입니다: $filePath"
}

New-Item -ItemType Directory -Force -Path $gamesDirectory | Out-Null
$template = @"
using System.Drawing;
using System.Windows.Forms;

namespace CSVParserTool.MiniGames
{
    [ExportMiniGame(
        "$GameId",
        "$escapedDisplayName",
        Description = "게임 설명을 입력하세요.",
        Controls = "조작 방법을 입력하세요.")]
    public sealed class $className : ExportMiniGame
    {
        protected override void OnCreate()
        {
            Context.SetScore(0);
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 매 프레임 게임 상태를 갱신합니다.
        }

        protected override void OnMouseDown(MouseButtons button, Point position)
        {
            Context.AddScore();
        }

        protected override void OnDraw(Graphics graphics, Rectangle viewport)
        {
            graphics.Clear(Context.Palette.Background);
            using (var brush = new SolidBrush(Context.Palette.Text))
                graphics.DrawString("$escapedDisplayName", SystemFonts.MessageBoxFont, brush, 16, 16);
        }
    }
}
"@

[IO.File]::WriteAllText($filePath, $template, (New-Object Text.UTF8Encoding($false)))
Write-Host "미니게임 생성 완료: $filePath"
Write-Host "다음 단계: OnUpdate / OnDraw / 입력 메서드를 구현한 뒤 GUI를 빌드하세요."
