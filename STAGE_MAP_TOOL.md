# 스테이지 맵 툴 사양

퍼즐 맵 제작 툴 구현 전 합의된 스펙을 기록한다.
`ToolScene` 구조와 컴포넌트 분해는 `STAGE_MAP_SCENE.md`를 따른다.
작업 시작 시 `MAP.md`, `DATA.md`, `INGAME.md`와 함께 확인한다.

---

## 목표

- Unity Editor에서 모드별 스테이지를 시각적으로 찍고 즉시 저장한다.
- 저장된 JSON은 런타임과 동일한 `StageData` / `CellData` 포맷을 사용한다.
- 빌드 전 기본 스테이지를 `Resources`에 포함하고, 런타임 다운로드 파일이 있으면 우선 사용한다.
- 이 툴은 **사이드 콘텐츠인 스테이지형 게임** 제작용이다. 메인 대전 퍼즐의 리플레이 다운로드/랭킹 흐름을 직접 변경하지 않는다.

---

## 콘텐츠 경계

- 메인 게임: 상대 유저 리플레이 다운로드/로드 → 플레이어와 상대 리플레이가 같은 조건에서 경쟁 → 점수 환산 → 랭킹 반영.
- 사이드 콘텐츠: `ThreeMatch`, `Link`, `TapMatch` 3가지 모드의 스테이지형 게임.
- `StageStorage`와 모드별 `Resources/Stage` 구조는 사이드 콘텐츠와 맵 툴 테스트 진입점에서 사용한다.
- `PopupReady` 같은 메인 대전 진입 UI에는 사이드 스테이지 선택 로직을 직접 연결하지 않는다.
- 리플레이 저장 시점에는 `StageData` JSON 스냅샷을 함께 저장하여, 다운로드 스테이지가 갱신되어도 재생 맵이 바뀌지 않게 한다.

---

## 로비 진입 흐름

사이드 스테이지는 로비의 별도 버튼에서 시작한다.

```text
LobbyScene
  → 사이드 콘텐츠 버튼 클릭
    → 모드별 스테이지 전체 팝업 열기
      → 스테이지 선택
        → StageInjection.MakeGameSpec(ruleAddress, puzzleType, stageId)
          → GameScene 또는 사이드 콘텐츠 전용 씬 이동
```

예시:

```text
원탭 스테이지 버튼 클릭
  → PopupTapMatchStage 생성
    → TapMatch Stage_001 선택
      → TapMatchRule + PuzzleType.TapMatch + stageId 1 로드
        → 스테이지 실행
```

권장 팝업:

- `PopupThreeMatchStage`
- `PopupLinkStage`
- `PopupTapMatchStage`

각 팝업은 해당 모드의 `1~100` 스테이지 목록, 잠금/클리어/별 상태, 시작 버튼을 담당한다.

---

## 스테이지 경로

기본 제공 파일:

```text
Assets/Resources/Stage/{Mode}/Stage_{000}.json
```

다운로드 파일:

```text
Application.persistentDataPath/Stage/{Mode}/Stage_{000}.json
```

모드 폴더:

```text
ThreeMatch
Link
TapMatch
```

예시:

```text
Assets/Resources/Stage/ThreeMatch/Stage_001.json
Application.persistentDataPath/Stage/ThreeMatch/Stage_001.json
```

---

## 로드 정책

`StageStorage`는 `PuzzleType`과 `stageId`를 받아 JSON 문자열을 반환한다.

1. `persistentDataPath/Stage/{Mode}/Stage_{000}.json`
2. `Resources.Load<TextAsset>("Stage/{Mode}/Stage_{000}")`

권장 API:

```csharp
StageStorage.TryLoadStageJson(PuzzleType puzzleType, int stageId, out string json)
StageInjection.MakeGameSpec(string ruleAddress, PuzzleType puzzleType, int stageId)
```

이 API는 사이드 스테이지형 콘텐츠 또는 맵 툴 `Save & Test`에서 사용한다.

---

## 스테이지 수

- 현재 스펙은 모드별 100개 스테이지를 기본값으로 한다.
- 전체 기본 더미 파일 수는 300개다.
- `stage_id`는 모드별 `1~100` 범위로 관리한다.
- 파일 번호와 `stage_id`는 동일해야 한다.

---

## 에디터 툴

메뉴:

```text
Tools/Puzzle/Stage Map Editor
```

필수 기능:

- 모드 선택: `ThreeMatch`, `Link`, `TapMatch`
- 스테이지 번호 선택: `1~100`
- Rule JSON 선택 또는 모드별 기본 Rule 자동 선택
- 현재 스테이지 로드
- 현재 스테이지 저장
- 없는 더미 스테이지 생성
- 그리드 기반 셀/블럭 페인팅
- 저장 후 `GameScene` 또는 사이드 콘텐츠 전용 진입점 테스트

브러시:

- 셀 타입: `Close`, `Normal`, `Lock`, `Generator`
- 블럭: 현재 Rule의 `blocks[]`에서 선택 또는 비우기
- 패널: `panel_id` 값 입력
- 생성기: `generator_block_ids` 다중 선택

---

## 검증

저장 전 다음 항목을 검사한다.

- 파일 번호와 `stage_id` 일치
- `stage_width * stage_height` 기준 셀 누락 여부
- 좌표 중복 여부
- 좌표 범위 초과 여부
- `block_id`가 현재 Rule의 `blocks[]`에 존재하는지 여부
- `generator_block_ids`가 현재 Rule의 `blocks[]`에 존재하는지 여부
- `Generator` 셀에 생성 목록이 비어 있는지 여부
- `Close` 셀에 블럭 또는 생성 목록이 남아 있는지 여부

추가 검증 후보:

- 3매치 초기 매칭 존재 여부
- 3매치 가능한 이동 존재 여부
- Link / TapMatch 모드별 플레이 가능성 검사

---

## 구현 순서

1. `StageStorage` 추가
2. `StageInjection`을 스테이지 Addressable 의존에서 `PuzzleType + stageId` 로드로 변경
3. 메인 대전 진입 UI와 분리된 사이드 콘텐츠 테스트 진입점 준비
4. 로비 사이드 버튼에서 모드별 스테이지 팝업 열기
5. 스테이지 팝업에서 선택한 모드/번호로 스테이지 실행
6. `Assets/Resources/Stage/{Mode}/Stage_001~100.json` 더미 생성
7. `StageMapEditorWindow` 추가
8. 검증과 `Save & Test` 연결

---

## 다음 작업 프롬프트

`STAGE_MAP_TOOL.md`와 `STAGE_MAP_SCENE.md` 기준으로 ToolScene 맵 툴 구조를 구현해줘. MAP/DATA/INGAME 문서 확인 후 진행.`
