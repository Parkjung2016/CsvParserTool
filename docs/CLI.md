# DataTool CLI 사용법

`DataTool.exe`는 GUI와 같은 XLSX 파싱, 버전 필터, enum, 배열, `ref`/`keyref`, 중복 Id 검증과 엔진별 Export 파이프라인을 명령줄에서 실행합니다. 빌드 서버나 반복 작업에서 사용할 수 있습니다.

## 기본 형식

```powershell
DataTool.exe export --project "프로젝트 루트" [옵션]
```

CLI는 GUI에 저장된 경로·엔진 설정을 사용하지 않습니다. 자동화 결과가 실행 환경에 따라 달라지지 않도록 `--project`, `--engine`, `--excel`을 명령에 명시하는 방식을 권장합니다.

## 옵션

| 옵션 | 필수 | 설명 |
|---|---:|---|
| `--project <경로>` | 예 | Unity 또는 Unreal 프로젝트 루트 |
| `--engine unity\|unreal` | 아니요 | Export 엔진. 생략하면 `unity` |
| `--excel <경로>` | 아니요 | `DT_*.xlsx` 원본 폴더. Unreal Export에는 지정 권장 |
| `--refresh-xlsx` | 아니요 | XLSX 원본 수를 검사하고 원본 없는 기존 산출물을 정리 |
| `--version <버전>` | 아니요 | 해당 버전 이하의 컬럼만 포함. 예: `1.0.0`. 생략하면 모든 컬럼 |
| `--no-orphan-cleanup` | 아니요 | `--refresh-xlsx` 사용 시 원본 XLSX가 없는 기존 산출물을 유지 |
| `--no-unreal-import` | 아니요 | Unreal C++ 코드만 만들고 Editor 타깃 컴파일과 UDataTable 자동 Import를 생략 |
| `-h`, `--help` | 아니요 | CLI 도움말 출력 |

경로에 공백이 있으면 반드시 큰따옴표로 감쌉니다.

## Unity Export

```powershell
DataTool.exe export `
  --engine unity `
  --project "D:\Game\MyUnityProject" `
  --excel "D:\GameData\Xlsx" `
  --refresh-xlsx `
  --version 1.0.0
```

프로젝트 루트에는 `Assets`와 `ProjectSettings` 폴더가 있어야 합니다.

생성 위치:

- C# 코드: `Assets/_Game/DataTables/Scripts`
- CSV: `Assets/_Game/DataTables/Content/CSV`
- MessagePack Bytes: `Assets/_Game/DataTables/Content/Bytes`

`--excel`을 생략하면 Unity 프로젝트의 기존 `Content/CSV/DT_*.csv`를 원본으로 다시 Export할 수 있습니다. 최신 XLSX 내용을 반영하려면 `--excel`을 지정합니다.

## Unreal Export

```powershell
DataTool.exe export `
  --engine unreal `
  --project "D:\Game\MyUnrealProject" `
  --excel "D:\GameData\Xlsx" `
  --refresh-xlsx `
  --version 1.0.0
```

프로젝트 루트에는 `.uproject` 파일이 하나 있어야 하며 Export 전에 해당 프로젝트의 Unreal Editor를 닫아야 합니다. 툴은 미저장 작업 보호를 위해 Editor를 강제로 종료하지 않습니다.

기본 동작:

1. XLSX와 모든 테이블 참조를 검증합니다.
2. `Source/{Module}/Public/DataTables/Generated`에 UENUM·USTRUCT와 런타임 헤더를 생성합니다.
3. Editor 타깃을 컴파일합니다.
4. XLSX 데이터를 메모리에서 UDataTable로 변환합니다.
5. `/Game/PJDevData/DataTables`에 UDataTable 에셋을 생성하거나 갱신합니다.

프로젝트에는 중간 CSV나 JSON을 남기지 않습니다. C++ 헤더는 IDE 또는 Unreal의 C++ Classes에서, UDataTable은 Content Browser에서 확인합니다.

코드 생성만 필요한 경우:

```powershell
DataTool.exe export `
  --engine unreal `
  --project "D:\Game\MyUnrealProject" `
  --excel "D:\GameData\Xlsx" `
  --no-unreal-import
```

이 옵션에서는 UDataTable이 갱신되지 않으므로 실제 게임 데이터 배포용 Export에는 기본 자동 Import를 사용합니다.

## 종료 코드와 자동화

| 종료 코드 | 의미 |
|---:|---|
| `0` | Export 성공 또는 도움말 출력 |
| `1` | 필수 옵션 누락, 잘못된 엔진 값, 데이터 검증 실패, 컴파일 또는 Import 실패 |

PowerShell 자동화 예시:

```powershell
& .\DataTool.exe export `
  --engine unity `
  --project $env:GAME_PROJECT_ROOT `
  --excel $env:GAME_DATA_XLSX `
  --refresh-xlsx `
  --version 1.0.0

if ($LASTEXITCODE -ne 0) {
    throw "DataTool Export 실패 (종료 코드: $LASTEXITCODE)"
}
```

표준 출력에는 진행 정보와 성공 요약이, 표준 오류에는 실패 원인이 기록됩니다. CI에서는 종료 코드와 표준 오류를 함께 보관하면 원인 추적이 쉽습니다.

## 자주 발생하는 실패

- `Missing --project`: `--project`가 없거나 빈 값입니다.
- 프로젝트 루트 오류: Unity는 `Assets`의 상위 폴더, Unreal은 `.uproject`가 있는 폴더를 지정합니다.
- Unreal Editor 실행 중: 저장하고 Editor를 종료한 뒤 다시 실행합니다.
- 참조 검증 실패: 오류에 표시된 테이블, 행 Id, 컬럼의 `ref` 또는 `keyref` 값을 확인합니다.
- enum 충돌: Unreal은 대소문자만 다른 EnumName이나 Value도 같은 엔진 이름으로 취급하므로 이름을 명확히 바꿉니다.