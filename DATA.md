# PuzleBattleGame 데이터/설정 참고 문서

JSON 규칙, 스테이지, GameSpec, 블럭 데이터 관련 작업 시 참고.

---

## 데이터 흐름

### 메인 대전 퍼즐

```
Rule JSON + ReplayData의 Stage JSON 참조
  → AssetManager.LoadAsset<TextAsset>(address)
    → JsonUtility.FromJson<T>()
      → StageInjection.MakeGameSpec()
        → GameSpec { StageData, RuleData, List<BlockData> }
          → PuzzleGameController.Start() 에서 소비
            → board.Initialize(gameSpec)
```

**StageInjection**: 싱글톤. `MakeGameSpec(ruleAddress, stageAddress)`로 JSON 로드 후 `GetGameSpec()`으로 반환. 파싱 실패 시 `false` 반환 + `_gameSpec = null`.

### 사이드 스테이지형 콘텐츠

```
Rule JSON + PuzzleType + stageId
  → StageStorage.TryLoadStageJson(puzzleType, stageId)
    → persistentDataPath/Stage/{Mode}/Stage_{000}.json 우선
    → 없으면 Resources/Stage/{Mode}/Stage_{000}
      → JsonUtility.FromJson<StageData>()
        → StageInjection.MakeGameSpec(ruleAddress, puzzleType, stageId)
          → GameSpec 주입
```

사이드 스테이지 로드 정책은 메인 대전의 리플레이 다운로드/랭킹 흐름과 직접 결합하지 않는다.

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

## GameSpec 구조

상세 필드 구조와 ReplayData 예시는 `DATA_SCHEMA.md` 참고.

---

## Rule JSON 구조

파일 위치: `Assets/05_Table/Rule/`
- `ThreeMatchRule.json` — 3매치 규칙
- `TapMatchRule.json` — 탭 매치 규칙
- `LinkMatchRule.json` — 링크 매치 규칙

```json
{
  "rule": {
    "ruleId": "ThreeMatch_Quadrangle",
    "puzzleType": 1,
    "boardShape": 1,
    "timeLimit": 0,
    "objectives": [
      { "type": 0, "count": 1000 },
      { "type": 1, "targetId": "100-1", "count": 20 }
    ]
  },
  "blocks": [
    {
      "blockId": "100-1",
      "inputType": 5,
      "destroyType": 2,
      "life": 1
    },
    {
      "blockId": "100-2",
      "inputType": 1,
      "destroyType": 2,
      "life": 1
    }
  ]
}
```

### puzzleType 값
| 값 | 모드 | 보드 클래스 |
|----|------|------------|
| 1 | ThreeMatch | ThreeMatchPuzzleBoard |
| 2 | Link | LinkPuzzleBoard |
| 3 | TapMatch | TapMatchPuzzleBoard |

### inputType 값 (비트 플래그)
| 값 | 의미 | 생성되는 블럭 |
|----|------|-------------|
| 1 | Swap | NormalBlock |
| 2 | Link | NormalBlock |
| 4 | Touch | NormalBlock |
| 5 | Swap+Touch | BombBlock |

---

## Stage JSON 구조

파일 위치:
- 기존/메인 호환: `Assets/05_Table/Stage/Stage.json`
- 사이드 콘텐츠 기본 스테이지: `Assets/Resources/Stage/{Mode}/Stage_{000}.json`
- 사이드 콘텐츠 다운로드 스테이지: `Application.persistentDataPath/Stage/{Mode}/Stage_{000}.json`

```json
{
  "stage_id": 1,
  "stage_width": 8,
  "stage_height": 8,
  "cells": [
    {
      "x": 0, "y": 0,
      "block_id": "100-1",
      "panel_id": 0,
      "cell_type": 1,
      "generator_block_ids": []
    },
    {
      "x": 0, "y": 7,
      "block_id": null,
      "panel_id": 0,
      "cell_type": 3,
      "generator_block_ids": ["100-1", "100-2", "100-3"]
    }
  ]
}
```

### cell_type 값
| 값 | 종류 | 설명 |
|----|------|------|
| 0 | Close | 비활성 셀 (블럭 배치 불가) |
| 1 | Normal | 일반 셀 |
| 2 | Lock | 잠금 셀 |
| 3 | Generator | 블럭 생성기 (보통 최상단 행) |

---

## 새 규칙 추가 방법

1. `Assets/05_Table/Rule/`에 새 JSON 파일 생성 (기존 형식 참고)
2. `puzzleType`에 새 값 할당
3. `PuzzleDefine.cs`의 `PuzzleType` 열거형에 값 추가
4. `IPuzzleBoard` 구현 클래스 작성 (`INGAME.md` 참고)
5. `PuzzleGameController.Start()`에서 PuzzleType별 분기 추가
6. Addressable에 JSON 등록

## 새 스테이지 추가 방법

1. `Assets/05_Table/Stage/Stage.json` 수정 또는 새 JSON 생성
2. `cells` 배열에 보드 크기만큼 셀 데이터 작성
3. 최상단 행은 `cell_type: 3` (Generator) + `generator_block_ids` 설정
4. `block_id`로 초기 블럭 배치 (null이면 빈 셀)

## 새 블럭 추가 방법

1. Rule JSON의 `blocks` 배열에 새 BlockData 추가
2. `blockId` 고유 값 지정
3. `inputType` 플래그 조합 설정
4. 필요 시 `INGAME.md` 참고하여 새 Block 클래스 + Factory 분기 추가
5. 블럭 스프라이트를 `Assets/04_Resources/Block/`에 추가 후 Addressable 등록

---

## ReplayData JSON 구조

파일 위치와 JSON 예시는 `DATA_SCHEMA.md` 참고.
