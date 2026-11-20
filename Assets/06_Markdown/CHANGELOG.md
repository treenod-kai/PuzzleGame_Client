# PuzleBattleGame 변경 이력 (Changelog)

기능별 주요 변경을 기록한다. 현재 동작과 코드 위치는 [문서 인덱스](README.md)에서 찾을 수 있다.

---

## 2026-11-20 — 로비 디펜스와 화면 대응

- 로비에 10×10 전장과 계정별 진행 저장을 연결했다. 확대 지도는 세로 10행을 표시하고 가로로 드래그한다. [로비 디펜스](01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md)
- 카드 인벤토리는 화면 주변의 카드 칸을 재사용하고, 상세 창에서 개별 강화와 같은 유닛 종류의 에너지 변환을 처리한다. [카드와 에너지](01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md#유저-데이터와-보유-유닛)
- 로비·팝업을 같은 기기 안전 영역에 맞추고, 작은 화면에서 대전·리플레이 상태와 터치 영역을 키웠다. [UI](04_UI/UI.md) · [화면 작업 지침](../../AGENTS.md#해상도-대응)
- 새 3매치 스테이지에서 초기 배치와 리필의 원형 폭탄을 제외했다. 이전 스테이지 리플레이는 기록 당시의 배치와 점수를 재현한다. [보드 규칙 버전 4](02_Ingame/INGAME_REPLAY.md#재생-흐름)

## 2026-04-30 — UI 버튼 및 맵툴 재작업 기반

### UI
- **UIButton**: 클릭 시 DOTween 스케일 피드백 추가.
- **UIButton**: 버튼 이미지 스프라이트 교체용 `SetSprite(Sprite)` 추가.
- **UIButtonGroup**: 인스펙터 캐싱된 버튼 배열에 normal/selected 스프라이트 적용.

### StageMapTool
- **StageMapToolController**: 퍼즐 타입 선택을 `StageMapToolState`와 연결.
- 마지막 퍼즐 타입은 새 맵 데이터가 없을 때만 `PlayerPrefs`에서 복원.
- **StageMapToolController**: 셀/블럭/타일 편집 모드 선택과 하위 패널 전환 추가.
- 기존 Runtime 뷰/입력/패널 스크립트와 `ToolScene` 제거, `Core` 모듈은 유지.
- 다음 작업 메모는 `STAGE_MAP_TOOL.md`에 정리.

---

## 2026-04-13 — 버그 수정 및 안전성 강화

### Null 안전성
- **LinkPuzzleBoard.Input()**: 링크 경로 마지막 셀 null 체크 + `Log()` 에러 출력
- **StageInjection.MakeGameSpec()**: JSON 파싱 결과 null 검증 (실패 시 `false` 반환)
- **PuzzleGameController.SaveReplay()**: `GetGameSpec()` null 체크
- **ThreeMatchPuzzleBoard.ProcessSwapInput()**: 스왑 대상 블럭 null 시 `Log()` 출력
- **AssetManager.LoadAsset\<T\>()**: 빈 주소 시 `LogError` 출력
- **ReplayStorage.Load()**: 파싱 결과/입력 기록/주소 누락 검증

### 최적화
- **FetchActions()**: 리스트 복사 → 참조 스왑으로 GC 제거 (3개 보드 전체)
- **AssetManager.ReleaseAll()**: LINQ → `_releaseBuffer` 재사용 루프
- **HasEmptyCell()**: `.Any()` → 수동 foreach
- **LinkPuzzleBoard**: `.Last()` → `[Count - 1]` 인덱스 접근
- **ExecuteBatchMovement**: Dictionary → `_batchActions`/`_batchViews` 병렬 리스트
- `using System.Linq` 제거 (LinkPuzzleBoard, ThreeMatchPuzzleBoard, AssetManager)

### 생명주기
- **PuzzleBoardView.OnDestroy()**: `StopAllCoroutines()` 추가

---

## 이전 이력 (요약)

- **인프라**: SharedScene 영구 상주, 씬 전환 파이프라인, 매니저 싱글톤 시스템
- **에셋**: AssetManager (Addressables 래핑, 캐싱, MarkPersistent/ReleaseAll), PoolManager
- **도메인 UI**: DomainManager 팝업/탭 스택, PopupBase/TabBase 생명주기
- **퍼즐 Model**: IPuzzleBoard 3종 (ThreeMatch/Link/TapMatch), 블럭 팩토리, 콤보/피버, 자동 셔플
- **퍼즐 View**: BatchMovement 처리 순서 보장, 애니메이션 (0.075s), LineRenderer 링크 경로
- **리플레이**: InputRecord/InputEndRecord 프레임 기록, ReplayController 축소 배치 재생
- **최적화**: LINQ 전면 제거, FloodFill/FindMatches 버퍼 재사용, ContactFilter2D 캐싱, LineRenderer 변경 감지
- **버그 수정**: ExecuteBatchMovement 블럭 미씽, GetPointerPosition null 크래시, ReplayStorage 모바일 경로, Main.cs 이벤트 릭, StageInjection 반환값 등
