# 스테이지 맵 툴씬 아키텍처

`ToolScene`에서 스테이지를 시각적으로 제작, 검증, 저장, 테스트하기 위한 구조를 정의한다.
기능 요구사항은 `STAGE_MAP_TOOL.md`, 데이터 포맷은 `DATA.md`, 보드/좌표 규칙은 `INGAME.md`를 따른다.

---

## 목표

- `ToolScene`은 사이드 스테이지형 콘텐츠의 맵 제작 전용 씬이다.
- 런타임 플레이 씬(`GameScene`)과 같은 `StageData` / `CellData` 포맷을 사용한다.
- 편집 중 데이터는 메모리 모델에 보관하고, 저장 시 JSON 파일로 직렬화한다.
- 저장 전 검증을 통과한 데이터만 `Assets/Resources/Stage/{Mode}/Stage_{000}.json` 또는 테스트용 다운로드 경로에 기록한다.
- `Save & Test`는 `StageInjection.MakeGameSpec(ruleAddress, puzzleType, stageId)`를 통해 실제 게임 진입 흐름을 검증한다.

---

## 씬 책임

`ToolScene`이 담당한다.

- 모드와 스테이지 번호 선택
- Rule JSON 로드 및 브러시 후보 구성
- Stage JSON 로드, 신규 생성, 저장
- 그리드 시각화와 셀 선택/페인팅
- 저장 전 검증 결과 표시
- 저장 후 `GameScene` 테스트 진입

`ToolScene`이 담당하지 않는다.

- 메인 대전 리플레이 다운로드/랭킹 흐름
- 로비 스테이지 진행도, 잠금, 보상 지급
- 실제 퍼즐 판정 로직 재구현
- `PopupReady` 같은 메인 대전 UI 수정

---

## 레이어 구조

```text
ToolScene
  Core
    StageMapToolState        - 현재 모드, 스테이지 번호, 브러시, 편집 데이터
    StageMapJsonRepository   - StageData 생성/로드/저장
    StageMapValidator        - 저장 전 검증
    StageMapCellBrush        - 셀 페인팅 입력값
  Runtime
    StageMapToolController   - 툴씬 흐름 조정
    StageMapGridView         - 셀/블럭/생성기 표시
    StageMapInputController  - 마우스 입력, 드래그 페인팅
    StageMapToolPanel        - 모드/스테이지/브러시/검증/저장 UI
    StageMapPlaytestLauncher - Save & Test 진입
```

`Core`는 맵 데이터 생성, 수정, 검증, 저장/로드까지만 담당한다.
`Core`는 `StageInjection`, `Main.MoveScene`, 로비 UI, 게임 실행 흐름을 호출하지 않는다.
`Runtime`은 `Core`를 사용해 ToolScene 입력, UI, 테스트 진입을 조립한다.
View와 UI는 `StageMapToolState`를 직접 수정하지 않고 Controller 메서드를 통해 요청한다.

---

## 데이터 흐름

### 로드

```text
모드/스테이지 선택
  -> StageMapJsonRepository.LoadOrCreate(puzzleType, stageId)
  -> StageData 파싱 또는 더미 생성
  -> StageMapToolState.SetStage(stageData)
  -> StageMapGridView.Rebuild(stageData)
  -> StageMapToolPanel.Refresh()
```

로드 우선순위는 `StageStorage`와 동일하게 유지한다.

1. `Application.persistentDataPath/Stage/{Mode}/Stage_{000}.json`
2. `Assets/Resources/Stage/{Mode}/Stage_{000}.json`

에디터에서 기본 리소스 파일을 직접 저장할 때는 `Assets/Resources/Stage/{Mode}/Stage_{000}.json`를 대상으로 한다.
플레이 중 임시 저장은 `Application.persistentDataPath/Stage/{Mode}/Stage_{000}.json`를 대상으로 한다.

### 페인팅

```text
셀 클릭/드래그
  -> StageMapInputController
  -> StageMapToolController.PaintCell(gridPos, brush)
  -> StageMapToolState의 CellData 수정
  -> StageMapGridView.RefreshCell(gridPos)
```

`Close` 셀로 바꿀 때는 `block_id`와 `generator_block_ids`를 비운다.
`Generator` 셀에는 생성 후보가 최소 1개 있어야 저장 검증을 통과한다.

### 저장

```text
저장 버튼
  -> StageMapValidator.Validate(stageData, ruleBlocks)
  -> 실패 시 StageMapToolPanel에 오류 표시
  -> 성공 시 StageMapJsonRepository.Save(stageData, targetPath)
  -> AssetDatabase.Refresh() (에디터 저장일 때만)
```

저장 시 `stage_id`는 현재 스테이지 번호로 강제 동기화한다.
셀 배열은 `y` 오름차순, `x` 오름차순으로 정렬해 JSON diff를 안정화한다.

### Save & Test

```text
Save & Test
  -> 저장 검증 및 저장
  -> StageMapPlaytestLauncher
  -> StageInjection.MakeGameSpec(ruleAddress, puzzleType, stageId)
  -> Main.MoveScene(ToolScene, GameScene)
```

테스트 진입은 사이드 스테이지 검증용이며, 상대 리플레이 데이터는 설정하지 않는다.

---

## 컴포넌트 책임

### StageMapToolController

- 툴씬의 진입점이다.
- UI 이벤트를 받아 State, Repository, Validator, View를 호출한다.
- 모드 변경, 스테이지 변경, 로드, 저장, 테스트를 단일 흐름으로 묶는다.
- 같은 오브젝트의 GridView, InputController, ToolPanel을 자동으로 찾거나 추가한다.

### StageMapToolState

- 현재 `PuzzleType`, `stageId`, `ruleAddress`, `StageData`, 브러시 상태를 보관한다.
- 씬 내 편집 상태만 담당하고 파일 IO를 하지 않는다.

### StageMapGridView

- `StageData.cells`를 기준으로 그리드를 그린다.
- 셀 타입, 블럭 ID, 패널 ID, 생성기 여부를 시각적으로 구분한다.
- 런타임 퍼즐 View와 분리한다. 실제 게임용 `PuzzleBoardView`를 편집 도구로 재사용하지 않는다.

### StageMapInputController

- `CameraController.MainCamera` 기준 포인터 좌표를 그리드 좌표로 변환한다.
- 클릭, 드래그, 브러시 적용 요청만 Controller에 전달한다.
- 페인팅 중 UI 위 포인터 입력은 무시한다.
- SharedScene 로드 타이밍 때문에 카메라가 없으면 매 프레임 다시 찾는다.

### StageMapToolPanel

- 모드, 스테이지 번호, Rule, 셀 타입, 블럭, 패널, 생성기 브러시를 표시한다.
- 검증 오류와 저장 결과를 표시한다.
- 저장 로직을 직접 수행하지 않는다.

### StageMapValidator

- `STAGE_MAP_TOOL.md`의 검증 항목을 구현한다.
- Rule의 `blocks[]`에 없는 `block_id`와 `generator_block_ids`를 오류로 처리한다.
- 경고와 오류를 분리하되, 오류가 있으면 저장을 막는다.

### StageMapJsonRepository

- `StageData` JSON 로드/저장을 담당한다.
- 경로 생성 규칙은 `StageStorage`의 모드 폴더와 파일명 규칙을 재사용한다.
- JSON 파싱 실패 시 null 대신 명확한 실패 결과를 반환한다.

### StageMapPlaytestLauncher

- Runtime 레이어에서 저장 성공 후 `StageInjection`과 `Main.MoveScene`을 호출한다.
- 테스트 시작 전 `StageInjection.SetReplayData(null)`로 리플레이 상태를 비운다.

---

## 구현 순서

1. `ToolScene` 진입 오브젝트와 `StageMapToolController` 골격을 만든다.
2. `StageMapToolState`와 `StageMapJsonRepository`로 로드/신규 생성/저장 경로를 고정한다.
3. `StageMapGridView`로 StageData를 읽어 셀 그리드를 표시한다.
4. `StageMapInputController`로 클릭/드래그 페인팅을 연결한다.
5. `StageMapToolPanel`에서 모드, 스테이지, 브러시, 저장 버튼을 연결한다.
6. `StageMapValidator`로 저장 전 오류를 막는다.
7. `StageMapPlaytestLauncher`로 `Save & Test`를 연결한다.
8. 씬 전환, 저장 파일, GameScene 진입을 수동 검증한다.

---

## 주의사항

- Model 레이어(`PuzzleCore/Module`)에는 툴씬 전용 코드를 넣지 않는다.
- `StageData` / `CellData` 구조를 툴 전용으로 바꾸지 않는다.
- Addressable Rule 로드는 기존 `AssetManager` 경유 원칙을 유지한다.
- `StageStorage`와 다른 경로 규칙을 만들지 않는다.
- `Core` 레이어에서 씬 이동, 게임 실행, 로비/팝업 흐름을 호출하지 않는다.
- `.meta`와 씬 파일은 필요한 경우에만 수정한다.
