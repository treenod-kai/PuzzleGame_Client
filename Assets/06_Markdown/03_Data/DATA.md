# PuzleBattleGame 데이터/설정 참고 문서

JSON 규칙, 스테이지, GameSpec, 블럭 데이터 관련 작업 시 참고.

## 콘텐츠와 C# 매핑

[전체 작업 지도](../MAP.md) · [문서 목록](../README.md)

파일 링크는 이 문서 기준 상대 경로이며, 마지막 열의 이름을 파일 안에서 검색하면 구현 위치를 찾을 수 있습니다.

| 찾는 콘텐츠 | 담당 C# 파일 | 주요 메서드·타입·멤버 |
|---|---|---|
| Rule/Stage 로드·사양 병합·시드 생성 | [StageInjection.cs](../../02_Scripts/StageMapTool/StageInjection.cs) | `MakeGameSpec`, `TryLoadRuleData`, `TryLoadStageData`, `GetGameSpec`, `SetReplayData` |
| 모드별 스테이지 번호·경로·JSON 로드 | [StageStorage.cs](../../02_Scripts/StageMapTool/StageStorage.cs) | `TryLoadStageJson`, `GetResourceKey` |
| 게임 사양·스테이지·셀 데이터 정의 | [GameSpec.cs](../../02_Scripts/PuzzleCore/Module/GameSpec.cs) | `GameSpec`, `GameSpec.mode`, `GameSpec.pivotSettings`, `CanSpawnBlock`, `GetBlockSpawnError`, `UseLegacyThreeMatchStageBombs`, `UseLegacyImmediateSwap`, `BattleFrameCount`, `StageData`, `HasValidCoordinates`, `CellData`, `GetBlock` |
| 규칙·블럭·목표 데이터, enum, 좌표, 입력 기록 | [PuzzleDefine.cs](../../02_Scripts/PuzzleCore/Module/PuzzleDefine.cs) | `GameRuleContainer`, `RuleData`, `BlockData.IsSpecial`, `ObjectiveData`, `GridPos`, `InputRecord`, `InputEndRecord` |
| 리플레이 저장 형식 | [ReplayData.cs](../../02_Scripts/PuzzleCore/Module/ReplayData.cs) | `ReplayData`, `IsCompleteBattleRecord`, `HasCompatibleBattleRules`, `pivotSettings`, `comboRuleVersion`, `numericRuleVersion`, `boardRuleVersion` |
| Rule JSON 및 리플레이에 보관하는 피벗 규칙 | [PivotSettings.cs](../../02_Scripts/Battle/Pivot/PivotSettings.cs) | `PivotSettings`, `IsValid`, `CopyValidated`, `HasSameRules` |
| 리플레이 JSON 저장·로드·저장 경로 | [ReplayStorage.cs](../../02_Scripts/PuzzleCore/Module/ReplayStorage.cs) | `Save`, `Load`, `GetReplayDirectoryPath` |
| 로비 디펜스 보드·카드 능력치·웨이브·경제 | [DefenseRules.cs](../../02_Scripts/LobbyDefense/Model/DefenseRules.cs) | 어드레서블 `LobbyDefense/Rules`, `Validate`, `GradeFor`. 카드의 선택 필드 `AttackProjectileAddress`는 기본 공격에 표시할 Sprite 주소다. [규칙 JSON](../../05_Table/LobbyDefenseRules.json) |

---

## 데이터 흐름

`ReplayData.pivotSettings`에는 플레이 시작 시 검증한 피벗 설정을 저장한다. 필드가 없는 기록은 기본값을 쓴다. `comboRuleVersion`은 콤보, `numericRuleVersion`은 시간·점수, `boardRuleVersion`은 배치·매칭·입력·블록 생성 규칙을 구분한다. [버전별 재생 규칙](../02_Ingame/INGAME_REPLAY.md#재생-흐름) · [피벗 모드](../02_Ingame/INGAME-VIEW-AND-BATTLE.md#대전-전용-피벗-모드)

```
Rule JSON + Stage JSON
  → AssetManager.LoadAsset<TextAsset>(address)
    → JsonUtility.FromJson<T>()
      → StageInjection.MakeGameSpec()
        → GameSpec { StageData, RuleData, List<BlockData>, pivotSettings }
          → PuzzleGameController.Start() 에서 소비
            → board.Initialize(gameSpec)
```

**StageInjection**: 싱글톤. `MakeGameSpec(ruleAddress, stageAddress, mode)`로 JSON 로드 후 `GetGameSpec()`으로 반환한다. `mode` 생략 시 Stage이며, 대전·대전 리플레이 진입은 Battle을 전달한다. `TryLoadRuleData`는 지원하는 퍼즐 타입과 블럭 목록을 확인하고, 블럭 ID나 수집 목표 ID가 비면 오류를 반환한다. 준비 실패 시 `MakeGameSpec`은 `_gameSpec`을 비우고 `false`를 반환한다. 대전은 `pivotSettings`가 없거나 유효하지 않아도 같은 방식으로 진입을 중단한다. 스테이지는 대전 전용 설정의 누락·범위 오류를 검사하지 않는다.

새 3매치 스테이지는 수집 목표량이 1개 이상일 때 대상 ID가 등록되어 있고 생성 가능한지 확인한다. [모드별 블록 생성 규칙](RULES-AND-STAGES.md#gimmickids-값-문자열-목록--gimmicktype--기믹-부착)

모드별 사이드 스테이지에는 `MakeGameSpec(ruleAddress, puzzleType, stageId)`를 사용한다. `StageStorage`가 다운로드 경로와 Resources를 차례로 조회한다. 리플레이 복원은 `stageJson`에 저장된 배치를 우선 사용한다.

### 데이터 타입 주의 (struct vs class)
| 타입 | 종류 | `== null` 가능 | 비고 |
|------|------|:---:|------|
| `GameRuleContainer` | class | O | JSON 파싱 실패 시 null 반환 |
| `RuleData` | **struct** | X | 파싱 실패 시 기본값(zero) — 컨테이너(`GameRuleContainer`) null 체크로 대체 |
| `ObjectiveData` | **struct** | X | |
| `GameSpec` | class | O | |
| `StageData` | class | O | JSON 파싱 실패 시 null 반환 |
| `CellData` | class | O | |
| `BlockData` | class | O | |
| `InputRecord` | **struct** | X | |
| `InputEndRecord` | **struct** | X | |
| `ReplayData` | class | O | |

---

## 스키마와 스테이지 편집

- [GameSpec과 리플레이 JSON 형식](DATA_SCHEMA.md)
- [규칙·스테이지 JSON과 피벗 설정](RULES-AND-STAGES.md)
- [모드별 스테이지 저장소와 편집 도구](DATA_STAGE.md)
