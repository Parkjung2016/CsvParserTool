# Unity / Unreal Export 구조

## 목표

XLSX 규칙, 타입 추론, 버전 필터, enum, 배열, `ref`/`keyref`, 중복 Id 검사는 엔진과 무관한 한 파이프라인에서 한 번만 수행한다. Unity와 Unreal은 검증이 끝난 공통 테이블 모델을 받아 자신의 산출물만 생성한다.

```text
XLSX / CSV
   ↓
Source Import
   ↓
CsvTableParseResult (공통 중간 모델)
   ↓
Reference · Enum · Id · Version Validation
   ↓
Engine Export Target
   ├─ Unity  → C# Container + MessagePack + Runtime Loader
   └─ Unreal → UENUM/USTRUCT + CSV Import + UDataTable Assets
```

## 계층과 책임

### Common

- `CsvTableParser`: XLSX에서 변환된 CSV를 공통 모델로 파싱한다.
- `CrossTableReferenceResolver`: 값 참조와 keyref를 엔진과 무관하게 검사한다.
- `EnumCatalogService`: enum 관리 XLSX를 공통 모델에 적용한다.
- `CsvTableParseResult`: Unity와 Unreal 생성기가 함께 사용하는 중간 모델이다.
- 엔진 이름, 경로, C#/C++ 문법을 이 계층에 넣지 않는다.

### Export Target

`IEngineExportTarget`이 프로젝트 판별, 프로젝트 검증, 출력 경로, 지원 기능을 소유한다.

- `UnityEngineExportTarget`
  - 프로젝트 표식: `Assets`, `ProjectSettings`
  - 코드: `Assets/_Game/DataTables/Scripts`
  - 데이터: `Assets/_Game/DataTables/Content`
- `UnrealEngineExportTarget`
  - 프로젝트 표식: 루트의 단일 `.uproject`
  - 코드: `Source/{ProjectName}/DataTables/Generated`
  - 데이터: `Content/PJDevData/DataTables`

UI와 CLI는 경로를 직접 조립하지 않고 `EngineExportTargetRegistry`에서 타깃을 얻는다. 자동 감지가 모호하면 사용자에게 Unity/Unreal을 직접 선택하게 한다.

### Target Generator

- Unity 생성기는 기존 `CsvClassGenerator`, `MessagePackTableExporter`, `UnityDataRuntimeGenerator`를 어댑터 안에서 호출한다.
- Unreal 생성기는 `UnrealCodeGenerator`를 사용한다.
- Unreal Editor Import는 별도 생성 단계로 유지한다. 외부 툴이 `.uasset` 바이너리를 직접 쓰지 않고 Unreal Commandlet 또는 Editor Subsystem을 실행해야 엔진 버전 호환성을 유지할 수 있다.

## Unreal 산출물 권장 형태

테이블마다 다음 파일을 생성한다.

- `{Table}Row.generated.h`: `UENUM`, `USTRUCT(BlueprintType)`, `FTableRowBase`
- `DT_{Table}.csv`: Unreal DataTable Import용 CSV
- 선택적으로 `{ProjectName}DataTableRegistry.generated.h/.cpp`: 런타임 조회 진입점

타입 매핑:

| 공통 타입 | Unreal 타입 |
|---|---|
| `bool` | `bool` |
| `uint` | `uint32` |
| `int` | `int32` |
| `float` | `float` |
| `double` | `double` |
| `string` | `FString` |
| `T[]` | `TArray<T>` |
| `CharacterType` enum | `ECharacterType` |

XLSX 타입 표기는 바꾸지 않는다. `enum:CharacterType`은 Unity에서 `CharacterType`, Unreal에서 `ECharacterType`으로 출력한다.

## Unreal Import 실행 방식

1. Data Tool이 검증된 CSV와 C++ 헤더를 원자적으로 생성한다.
2. Unreal 프로젝트에 제공할 Editor 플러그인이 manifest를 읽는다.
3. Commandlet/Editor Subsystem이 `UDataTable`을 생성 또는 갱신한다.
4. Import 오류를 JSON 결과로 반환한다.
5. Data Tool은 결과를 기존 Export 결과 표에 합친다.

이 방식은 Unreal이 실행 중이 아니면 Commandlet, 실행 중이면 Editor Subsystem을 사용할 수 있게 한다.

## 단계별 적용 순서

1. 현재 Unity 흐름을 `UnityExportEmitter`로 이동하되 생성 결과가 완전히 같은지 golden test로 비교한다.
2. GUI와 CLI에 `ExportPlatform`을 추가하고 기본값은 Unity로 유지한다.
3. Unreal C++/CSV 생성 및 fixture test를 추가한다.
4. Unreal Editor 플러그인과 Commandlet Import를 추가한다.
5. 공통 Export 결과에 타깃 단계와 Unreal Import 결과를 표시한다.

## 금지할 결합

- `DataExportService` 내부에서 `if (platform == Unreal)` 분기를 계속 늘리지 않는다.
- Unreal 생성기가 XLSX를 다시 파싱하거나 참조를 별도로 검사하지 않는다.
- GUI가 Unity/Unreal 출력 경로를 직접 계산하지 않는다.
- 외부 툴에서 `.uasset` 파일 포맷을 직접 생성하지 않는다.