# PuzleBattleGame 데이터 스키마 상세

[데이터 흐름](DATA.md) · [스테이지 저장소](DATA_STAGE.md)

## GameSpec 구조

```
GameSpec
├─ mode (GameMode: 0=Stage, 1=Battle)
├─ pivotSettings (PivotSettings) — Rule JSON에서 전달한 대전 전용 피벗 규칙
├─ StageData
│   ├─ stage_id        (int) — 스테이지 번호
│   ├─ stage_width     (int) — 보드 가로 크기
│   ├─ stage_height    (int) — 보드 세로 크기
│   └─ List<CellData>
│       ├─ x, y                  (int) — 셀 좌표
│       ├─ block_id              (string) — 초기 블럭 ID (null이면 비어있음)
│       ├─ panel_id              (int) — 바닥 패널 종류
│       ├─ cell_type             (int) — CellType 열거형 값
│       └─ generator_block_ids   (List<string>) — Generator 셀의 생성 블럭 목록
│
├─ RuleData
│   ├─ ruleId          (string) — 규칙 식별자
│   ├─ puzzleType      (int) — 1:ThreeMatch, 2:Link, 3:TapMatch
│   ├─ boardShape      (int) — 1:Quadrangle, 2:Hexagon
│   ├─ timeLimit       (float) — 제한 시간 (초), 0이면 무제한
│   └─ List<ObjectiveData>
│       ├─ type         (int) — 0:Score, 1:CollectBlock
│       ├─ targetId     (string) — 대상 blockId (CollectBlock일 때)
│       └─ count        (int) — 목표 값
│
├─ randomSeed        (int) — 결정론적 리플레이를 위한 난수 시드 (StageInjection에서 자동 생성)
│
└─ List<BlockData>
    ├─ blockId         (string) — 블럭 식별자 (예: "100-1")
    ├─ inputType       (List<string>) — 조작 방법 문자열 목록 (예: ["Swap","Touch"]) → InputType 플래그로 변환
    ├─ destroyType     (int) — 파괴 방식
    ├─ life            (int) — 내구도
    └─ gimmickIds      (List<string>) — 부착할 기믹 id 목록 (예: ["Bomb"]). 없거나 비어있으면 기믹 없음
```

---

[규칙과 스테이지 데이터](RULES-AND-STAGES.md)에서 상세 내용을 확인한다.

## ReplayData JSON 구조

파일 위치:
- **에디터**: `Assets/05_Table/Replay/replay_{timestamp}.json`. 플레이 기록과 Unity가 생성한 짝 파일(`.json.meta`)은 프로젝트에 누적되며 Git에서 제외한다.
- **빌드**: `Application.persistentDataPath/Replay/replay_{timestamp}.json`

게임 종료 시 `ReplayStorage.Save()`에 의해 자동 생성됨.

```json
{
    "formatVersion": 1,
    "numericRuleVersion": 1,
    "comboRuleVersion": 1,
    "mode": 1,
    "timeLimit": 90,
    "frameCount": 4500,
    "finalScore": 30,
    "ruleAddress": "LinkMatchRule",
    "stageAddress": "Stage",
    "randomSeed": 2095364872,
    "inputs": [
        { "frame": 67, "position": { "X": 3, "Y": 6 } },
        { "frame": 90, "position": { "X": 3, "Y": 5 } }
    ],
    "inputEnds": [
        { "frame": 115 },
        { "frame": 240 }
    ],
    "recordedAt": "2026-04-01T20:37:34+09:00"
}
```

| 필드 | 타입 | 설명 |
|------|------|------|
| `formatVersion` | int | 현재 1. 없으면 이전 형식 0으로 읽음 |
| `comboRuleVersion` | int | 현재 1은 유저 조작 성공만 콤보로 계산. 없는 기존 기록의 0은 당시 자동 매칭/파괴 포함 규칙을 복원 |
| `mode` | GameMode | Stage(0), Battle(1) |
| `timeLimit` | float | 기록 당시 제한 시간(초). 유한·음이 아닌 값이며 정수 틱 범위 이내 |
| `numericRuleVersion` | int | 현재 1: 시간 표현 오차 보정·정수 점수. 누락된 기존 기록의 0: 이전 float 계산 복원 |
| `frameCount` | ulong | 실제 완료한 총 틱 수. 대전은 4,500 |
| `finalScore` | int | 재생 결과와 비교할 최종 점수. 새 규칙은 int.MaxValue에서 포화 |
| `pivotSettings` | PivotSettings | 대전 시작 당시 피벗 설정 복사본. 없는 기존 기록은 기본값 사용 |
| `ruleAddress` | string | 규칙 JSON의 Addressable 에셋 주소 |
| `stageAddress` | string | Addressable 스테이지 주소 또는 모드별 StageStorage 리소스 키 |
| `stageJson` | string | 기록 시점의 스테이지 배치 스냅샷. 누락된 경우 주소를 사용한다. |
| `randomSeed` | int | 게임에 사용된 난수 시드 |
| `inputs` | List | 유저 입력 기록 (프레임 + 그리드 좌표) |
| `inputEnds` | List | 유저 입력 종료 기록 (프레임) |
| `recordedAt` | string | 기록 일시 (ISO 8601) |

### 리플레이 입력 순서
- 새 `InputRecord`와 `InputEndRecord`는 공통 증가 순서인 `ulong sequence`를 저장한다. 같은 고정 틱 안에서 좌표 입력과 손 떼기 이벤트가 섞여도 원래 순서로 복원한다.
- 기록 프레임은 입력 판정을 실행한 고정 틱이며 0부터 시작한다. 이전 형식의 기록은 읽을 수 있지만 이전 가변 프레임 게임 결과의 완전한 재현은 보장하지 않는다.

### 목표 종류와 셀 속성
- 목표는 `Score`(0), `CollectBlock`(1)을 지원한다. 그 외 목표 값은 달성으로 판정하지 않는다.
- 맵의 비활성 영역은 `CellType.Close`(0), 로직상 잠긴 셀은 `CellType.Lock`(2)이다. 셀 속성과 목표 종류는 별개의 데이터다.

### 대전 기록 호환성
- 상대 선택에는 현재 규칙의 90초·4,500틱 완주 대전 기록을 사용한다. 60초·3,000틱 완주 기록은 원래 길이로 재생할 수 있다. 스테이지 기록은 대전 기록으로 변환하지 않는다.
- `ReplayStorage.Load()`에서 형식 버전, 모드, 시간, 완료 기록을 검사한다. 향후 알 수 없는 버전은 거부한다.
- 대전 기록의 `pivotSettings`가 존재하면 유효 범위도 검사한다. 잘못된 스냅샷은 상대 선택 전에 제외하며, 필드가 없는 기존 기록은 허용한다.
- 새 플레이는 `comboRuleVersion: 1`로 저장한다. 버전 0의 3매치 재생은 자동 매칭도 기존 점수 콤보에 포함하며, 피벗 콤보도 기존의 파괴 발생 기준을 사용한다. 이를 위한 `GameSpec.UseLegacyAutomaticCombos`는 재생 시에만 설정하는 비직렬화 필드다. 알 수 없는 콤보 버전은 로드에서 거부한다.
- `IsCompleteBattleRecord()`는 재생 가능한 완주 기록인지 검사하고, `HasCompatibleBattleRules()`는 현재 점수 조건으로 대전할 수 있는지 별도로 검사한다. `LobbyMain.PrepareBattle()`은 슬롯에서 선택한 Rule의 기록 중 현재 콤보·수치 계산 버전과 해당 Rule JSON의 피벗 설정이 모두 같은 상대만 선택한다. 결과 표시 때도 같은 조건을 검사하므로, 다른 규칙의 기록을 직접 재생해도 승패를 비교하지 않는다.
- 동일 주소의 Rule/Stage가 바뀌면 기존 결과와 달라질 수 있다. 종료 틱/점수 검증 실패 시 승패를 비교하지 않는다. 스테이지 스냅샷이 있는 기록은 저장된 배치를 복원한다. Rule 데이터는 주소로 로드한다.
- 문서 JSON 예제의 입력과 점수는 필드 설명용이며 실제 대전 상대는 게임에서 완주하여 저장한 기록을 사용한다.

- 매초 10개 파괴·매칭 1회를 기준으로 피벗은 24·55·86초에 발동한다. `SquareNeighborClear`는 3매치·탭 매치에서 true이며 이전 기록에서 생략하면 false다. 현재 밸런스와 구형 기록의 기본값은 [피벗 게임 데이터](RULES-AND-STAGES.md#피벗-게임-데이터)를 참고한다.
- `GameSpec.UseRecordedBattleDuration`은 리플레이 재생 중 기록 당시 제한 시간을 복원한다.
