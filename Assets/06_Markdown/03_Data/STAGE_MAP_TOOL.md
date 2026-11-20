# 스테이지 맵 툴 작업 메모

[ToolScene](../../01_Scenes/ToolScene.unity)은 빌드 목록에서 제외된 에디터 전용 씬이다. 퍼즐 타입과 편집 모드를 선택할 수 있다. 현재 씬의 컨트롤러에는 보드 뷰가 연결되지 않아 스테이지 로드와 격자 표시가 시작되지 않는다.

## 현재 상태

| 기능 | 구현 및 씬 연결 상태 |
|---|---|
| 퍼즐 타입·편집 모드 | 두 버튼 그룹을 연결했다. 시작 시 `PlayerPrefs`에서 마지막 퍼즐 타입을 복원한다. |
| 격자·셀 클릭 | `StageMapBoardView`와 `StageMapCellView`가 구현되어 있다. 셀 프리팹·셀 루트·컨트롤러의 `_boardView`를 연결하면 스테이지를 읽고 클릭한 한 칸을 갱신한다. |
| 셀 브러시 | 셀 타입 변경 콜백이 구현되어 있다. 셀·블럭·타일 패널과 셀 타입 버튼 그룹은 씬에 연결해야 한다. |
| 블럭·타일·저장 | 규칙별 블럭/생성 목록과 타일 선택, 스테이지 번호 선택, 검증·저장 및 테스트 플레이 UI는 후속 작업이다. |

## 버튼 콜백

| 컨트롤러 메서드 | 버튼 값 순서 |
|---|---|
| `OnClickPuzzleType` | 0 ThreeMatch · 1 TapMatch · 2 Link |
| `OnClickEditMode` | 0 셀 · 1 블럭 · 2 타일 |
| `OnClickCellType` | 0 Normal · 1 Close · 2 Lock · 3 Generator |

셀 타입 버튼은 표의 인덱스를 사용한다. 격자를 클릭하면 `PaintCell`이 현재 브러시의 셀 타입·블럭·패널·생성 목록을 해당 칸에 적용한다.

[씬 배선과 클릭 검증](STAGE_MAP_TOOL_SCENE_SETUP.md) · [후속 기능 설계](STAGE_MAP_TOOL_ARCH.md) · [저장소와 검증 규칙](DATA_STAGE.md)
