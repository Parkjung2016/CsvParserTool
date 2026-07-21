# Export Mini Game Framework

Export가 백그라운드에서 진행되는 동안 플레이할 수 있는 WinForms 기반 미니게임 프레임워크입니다.
게임은 Unity와 비슷한 생명주기 메서드를 사용하며 모든 게임 코드와 리소스는 최종 `DataToolGUI.exe`에 포함됩니다.
실행 폴더에 미니게임 DLL이나 `MiniGames` 리소스 폴더를 만들지 않습니다.
Export를 시작할 때 등록된 게임 중 하나를 균등하게 랜덤 선택하며, 창 상단에서 다른 게임으로 바꿀 수도 있습니다.

## 가장 빠르게 게임 만들기

저장소 루트에서 다음 명령을 실행합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\New-MiniGame.ps1 -Name CoinCatch -DisplayName "코인 캐치"
```

생성된 `MiniGames/Games/CoinCatchMiniGame.cs`에서 아래 메서드를 구현합니다.

- `OnCreate`: 최초 오브젝트와 상태 생성
- `OnStart`: 게임 시작 또는 다시 시작
- `OnUpdate(float deltaTime)`: 매 프레임 상태 갱신, `deltaTime`은 초 단위
- `OnDraw(Graphics, Rectangle)`: 화면 그리기
- `OnKeyDown`, `OnMouseDown` 등: 입력 처리
- `OnExportStateChanged`: Export 진행·완료 상태 반영
- `OnStop`, `OnDispose`: 사용한 리소스 정리

클래스에는 하나의 통일된 등록 규칙을 사용합니다.

```csharp
[ExportMiniGame(
    "my-company.coin-catch",
    "코인 캐치",
    Description = "떨어지는 코인을 받으세요.",
    Controls = "← → 또는 A D")]
public sealed class CoinCatchMiniGame : ExportMiniGame
{
    protected override void OnUpdate(float deltaTime) { }
    protected override void OnDraw(Graphics graphics, Rectangle viewport) { }
}
```

별도 등록 코드는 필요 없습니다. 현재 GUI 프로젝트 안의 게임 클래스를 자동 검색합니다.

## 자주 사용하는 API

- `Context.Input.IsKeyDown(Keys.Left)`: 누르고 있는 키 확인
- `Context.Input.MousePosition`: 현재 마우스 위치
- `Context.Random`: 게임 전용 난수
- `Context.ViewportSize`: 현재 게임 화면 크기
- `Context.Palette`: 현재 앱 테마와 맞는 색상
- `Context.IsExportRunning`: Export 진행 여부
- `Context.SetScore(value)`, `Context.AddScore()`: 점수 UI 갱신
- `Context.RequestRepaint()`: 즉시 다시 그리기 요청

모든 생명주기 콜백은 UI 스레드에서 실행됩니다. `OnUpdate`와 `OnDraw`에서는 파일 읽기나
긴 계산을 하지 말고, 큰 이미지와 폰트는 `OnCreate`에서 한 번만 준비한 뒤 `OnDispose`에서 정리하세요.

## 공통 난이도 구조

난이도를 지원할 게임은 `IConfigurableMiniGameDifficulty`를 구현합니다.

```csharp
public IReadOnlyList<MiniGameDifficultyOption> DifficultyOptions { get; }
public string CurrentDifficultyId { get; }
public void SetDifficulty(string difficultyId) { }
```

프레임워크가 옵션을 감지하면 게임 창에 난이도 목록을 자동 표시합니다. `DataVolleyMiniGame.cs`의
`DifficultyPresets` 배열에서 다음 값만 바꾸면 AI 난이도를 조절할 수 있습니다.

- `aiMoveSpeed`: AI 이동 속도
- `reactionSeconds`: 판단 주기, 작을수록 빠르게 반응
- `predictionError`: 낙하지점 예측 오차
- `jumpDistance`: 점프 공격을 시도하는 거리
- `jumpChance`: 점프 시도 확률
- `hitBoost`: AI가 공을 받아칠 때 추가되는 힘

현재 프리셋은 쉬움·보통·어려움이며 배구의 기본값은 어려움입니다. 프리셋을 배열에 추가하면 UI에도 자동 추가됩니다.

## 내장 배구게임

- 플레이어는 왼쪽, AI는 오른쪽에서 경기합니다.
- `← →` 또는 `A D`로 이동하고 `↑`, `W`, `Space`로 점프합니다.
- 먼저 7점을 얻으면 승리하고 잠시 후 새 경기가 시작됩니다.
- 캐릭터와 공은 코드로 직접 그리므로 별도 이미지나 리소스 파일이 필요하지 않습니다.
## 빌드와 배포

1. 게임 소스와 필요한 이미지를 GUI 프로젝트에 추가합니다.
2. 이미지가 필요하면 프로젝트에 `EmbeddedResource`로 등록해 EXE 내부에서 읽습니다.
3. `dotnet build CSVParserTool.slnx -c Release`로 빌드합니다.
4. Costura가 프레임워크와 게임 코드를 `DataToolGUI.exe` 하나에 포함합니다.
5. 사용자 실행 폴더에는 `DataToolGUI.exe`와 설정 XML만 유지합니다.

## 프레임워크 규칙

- 게임 ID는 겹치지 않도록 `회사.게임이름` 형식을 권장합니다.
- 같은 ID가 여러 개면 먼저 발견된 게임만 등록됩니다.
- 매개변수 없는 public 생성자가 필요합니다.
- 한 게임의 오류는 Export를 실패시키지 않으며 해당 게임 루프만 중지됩니다.
- 프레임 간격은 약 16ms이고, 큰 지연은 최대 50ms로 제한됩니다.
- 게임 창을 닫아도 Export는 계속 진행됩니다.