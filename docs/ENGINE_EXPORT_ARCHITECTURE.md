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
   └─ Unreal → UENUM/USTRUCT 헤더 + 메모리 CSV payload → UDataTable
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
  - 기본 런타임 모듈: `.uproject`의 `Modules`와 `Source/**/*.Build.cs`를 함께 검사
  - 생성 C++: `Source/{ModuleName}/DataTables/Generated` (헤더와 cpp 통합)
  - 중간 데이터: 파일을 생성하지 않고 메모리 CSV 문자열로 Commandlet에 전달
  - Import 대상 패키지: `/Game/PJDevData/DataTables`

UI와 CLI는 경로를 직접 조립하지 않고 `EngineExportTargetRegistry`에서 타깃을 얻는다. 자동 감지가 모호하면 사용자에게 Unity/Unreal을 직접 선택하게 한다.

### Target Generator

- Unity 생성기는 기존 `CsvClassGenerator`, `MessagePackTableExporter`, `UnityDataRuntimeGenerator`를 어댑터 안에서 호출한다.
- Unreal 생성기는 `UnrealCodeGenerator`를 사용한다.
- Unreal Editor Import는 별도 생성 단계로 유지한다. 외부 툴이 `.uasset` 바이너리를 직접 쓰지 않고 Unreal Commandlet 또는 Editor Subsystem을 실행해야 엔진 버전 호환성을 유지할 수 있다.

## Unreal 산출물 권장 형태

테이블마다 다음 파일을 생성한다.

- `{Table}Row.h`: `UENUM`, `USTRUCT(BlueprintType)`, `FTableRowBase` (UHT가 `.generated.h` 생성)
- `GlobalDataStorage.h/.cpp`: 같은 Generated 폴더에 생성되는 GameInstance 단위 자동 로드와 타입 안전 조회
- `InfoStorage.h`: 여러 원본 테이블을 게임용 데이터로 가공하는 `IInfoStorage`와 자동 Registry

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
Unreal 리플렉션 이름은 대소문자만 다른 식별자를 구분하지 못한다. EnumName 또는 같은 enum의 Value가 case-only로 충돌하면 Unreal Preview와 Export를 실패 처리하고, 충돌한 두 이름을 오류에 표시한다. XLSX와 Unity의 대소문자 구분은 변경하지 않는다.


## Unreal Import 실행 방식

1. Data Tool이 전체 검증을 통과한 C++ 헤더와 메모리 CSV payload를 준비한다.
2. `.uproject`의 `EngineAssociation`, Windows 등록 정보, Epic Launcher manifest에서 정확한 Unreal 설치를 찾는다.
3. 프로젝트의 `*Editor.Target.cs` 타깃을 빌드하여 생성된 USTRUCT/UENUM을 UHT에 반영한다.
4. 닫힌 Unreal Editor를 `PythonScriptCommandlet` 모드로 실행하고 메모리 문자열을 `UDataTable`로 생성·갱신한다.
5. CSV Import Key Field는 `Id`, 대상 패키지 기본값은 `/Game/PJDevData/DataTables`다.
6. 시스템 임시 폴더의 Import 결과를 확인한 뒤 Export 성공 처리하고 임시 스크립트와 결과를 즉시 삭제한다. `.uasset` 바이너리는 Data Tool이 직접 쓰지 않고 Unreal API로 저장한다.

리플렉션 대상 `USTRUCT`/`UENUM` 헤더가 바뀌는 동안에는 해당 프로젝트의 Unreal Editor를 닫아야 한다. GUI는 실행 중인 Editor를 감지하면 저장 후 종료하도록 안내하고, CLI/Core Export는 오류로 중단한다. 저장하지 않은 작업을 보호하기 위해 프로세스를 강제 종료하지 않는다.

`Source/{ModuleName}/DataTables/Generated`의 `.h`는 C++ 코드이므로 일반 Content Browser 에셋 목록에 표시되지 않는다. IDE 또는 Content Browser의 `C++ Classes` 표시 옵션에서 확인하며, 정상 컴파일이 끝나야 갱신된다. `Content/PJDevData/DataTables`에는 바로 사용할 수 있는 `.uasset` 형태의 `UDataTable`이 생성된다.

CLI에서 C++ 코드만 생성해야 하는 특수한 경우에는 `--no-unreal-import`를 사용한다. GUI와 일반 CLI Export는 자동 Import가 기본값이다.


## Unreal 런타임 접근

`UGlobalDataStorage`는 `UGameInstanceSubsystem`으로 생성되며 Export된 원본 UDataTable을 보관한다. 게임 규칙에 맞춘 조합, 그룹, 빠른 검색 Map은 사용자 정의 `IInfoStorage`에서 만든다. 생성 파일은 프레임워크만 제공하므로 사용자 코드는 Export 때 덮어쓰지 않는다.

```cpp
// GameStatInfoStorage.h
#include "DataTables/Generated/InfoStorage.h"

class FGameStatInfoStorage final : public IInfoStorage
{
public:
    void Build(const UGlobalDataStorage& Data) override
    {
        TArray<FStatDefinitionRow> Rows;
        Data.GetAllStatDefinition(Rows);

        ByStatId.Reset();
        for (const FStatDefinitionRow& Row : Rows)
            ByStatId.Add(FName(*Row.StatId), Row);
    }

    void Clear() override
    {
        ByStatId.Reset();
    }

    const FStatDefinitionRow* Find(FName StatId) const
    {
        return ByStatId.Find(StatId);
    }

private:
    TMap<FName, FStatDefinitionRow> ByStatId;
};
```

```cpp
// GameStatInfoStorage.cpp
#include "GameStatInfoStorage.h"

REGISTER_INFO_STORAGE(FGameStatInfoStorage);
```

원본 테이블 로드가 끝나면 등록된 모든 Storage의 `Build`가 자동 호출된다. 이후에는 다음처럼 가공 데이터를 가져온다.

```cpp
const FGameStatInfoStorage* Stats =
    FInfoStorageRegistry::Get<FGameStatInfoStorage>();
const FStatDefinitionRow* Health = Stats ? Stats->Find(TEXT("Health")) : nullptr;
```

## 단계별 적용 순서

1. Unity와 Unreal의 공통 검증 모델을 유지한다.
2. Unreal C++ 생성 결과와 메모리 Import payload를 fixture test로 검증한다.
3. 실제 Unreal 프로젝트에서 Editor 타깃 빌드와 UDataTable 생성·갱신을 통합 검증한다.
4. 공통 Export 결과에 컴파일과 Import 실패 원인을 표시한다.
5. 엔진 버전별 Python API 차이는 실제 설치된 EngineAssociation 기준으로 검증한다.

## 금지할 결합

- `DataExportService` 내부에서 `if (platform == Unreal)` 분기를 계속 늘리지 않는다.
- Unreal 생성기가 XLSX를 다시 파싱하거나 참조를 별도로 검사하지 않는다.
- GUI가 Unity/Unreal 출력 경로를 직접 계산하지 않는다.
- 외부 툴에서 `.uasset` 파일 포맷을 직접 생성하지 않는다.
