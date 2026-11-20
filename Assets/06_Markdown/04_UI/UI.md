# PuzleBattleGame UI/도메인 시스템 참고 문서

UI, 팝업, 탭 관련 작업 시 이 문서를 참고.

## 콘텐츠와 C# 매핑

[전체 작업 지도](../MAP.md) · [문서 목록](../README.md)

| 찾는 콘텐츠 | 담당 C# 파일 | 주요 메서드·타입·멤버 |
|---|---|---|
| 팝업/탭 스택·뒤로 가기·전체 닫기 | [DomainManager.cs](../../02_Scripts/Manager/DomainManager.cs) | `OpenPopup`, `OpenTab`, `Back`, `CloseAll` |
| 팝업/탭 공통 계약 | [IDomainNode.cs](../../02_Scripts/Manager/IDomainNode.cs) | `IDomainNode` |
| 팝업 생성·파괴·씬별 등록 | [PopupController.cs](../../02_Scripts/Manager/PopupController.cs) | `CreatePopup`, `DestroyPopup` |
| 팝업 생명주기·애니메이션·안전 영역 배치 | [PopupBase.cs](../../02_Scripts/Manager/PopupBase.cs) | `Open`, `Close`, `OpenRoutine`, `CloseRoutine`, `FitSafeArea` |
| 팝업 버튼 처리용 기반 클래스 | [PopupHandler.cs](../../02_Scripts/Manager/PopupHandler.cs) | `ClosePopup`, `BackPopup` |
| 탭 검색·활성화·비활성화 | [TabController.cs](../../02_Scripts/Manager/TabController.cs) | `ActivateTab`, `DeactivateTab` |
| 탭 생명주기·애니메이션 확장 | [TabBase.cs](../../02_Scripts/Manager/TabBase.cs) | `Activate`, `Deactivate`, `ActivateAnimation`, `DeactivateAnimation` |
| Inspector 버튼 콜백 연결 | [UIButton.cs](../../02_Scripts/UI/UIButton.cs) | `OnClickEvent` |
| 90초 대전 준비·슬롯 연출·게임 진입 | [PopupReady.cs](../../02_Scripts/UI/Popup/PopupReady.cs) | `OnClickStart`, `SpinAndStart`, `OnClickClose` |
| 로비 대전 진입·리플레이 상대 선택 | [LobbyMain.cs](../../02_Scripts/Lobby/LobbyMain.cs) | `OnClickBattle`, `PrepareBattle` |
| 준비 팝업 라이프·오프라인 충전 | [PlayerLives.cs](../../02_Scripts/Lobby/PlayerLives.cs) | `GetRemaining`, `TrySpend` |
| Shared 공용 팝업 등록 | [SharedPopupController.cs](../../02_Scripts/Manager/SharedPopupController.cs) | `ControllerName` |
| 로비 팝업 등록·로비 직접 실행 시 매니저 대기 | [LobbyPopupController.cs](../../02_Scripts/Lobby/LobbyPopupController.cs) | `ControllerName`, `RegisterWhenReady` |
| 게임 팝업 등록 | [GamePopupController.cs](../../02_Scripts/PuzzleCore/Controller/GamePopupController.cs) | `ControllerName` |
| 로비 안전 영역·하단 다섯 탭·스와이프·모달 입력 | [LobbyNavigation.cs](../../02_Scripts/Lobby/LobbyNavigation.cs) | `FitSafeArea`, `SelectTab`, `OnEndDrag`, `SwitchBySwipe` |
| 오버레이 패널의 가로 전환 | [LobbyOverlayTab.cs](../../02_Scripts/Lobby/LobbyOverlayTab.cs) | `Bind`, `ActivateAnimation`, `DeactivateAnimation` |
| 메인 화면의 상주 탭 컴포넌트 | [LobbyTabController.cs](../../02_Scripts/Lobby/LobbyTabController.cs) | `LobbyTabController` |
| 대전 전용 피벗 충전·활성 게이지 | [PivotGaugeView.cs](../../02_Scripts/Battle/Pivot/PivotGaugeView.cs) | `PivotGaugeView`, `SetLayout`, `Refresh`, `Dispose` |

대전의 점수·타이머·피벗 게이지와 퍼즐 안내는 화면의 안전 영역 안에서 보드와 따로 배치한다. 피벗 게이지는 타이머와 같은 폰트를 사용한다. 게임 종료 시 숨기고 컨트롤러 제거 시 UI를 정리한다. 상태 및 설정 규칙은 [INGAME-VIEW-AND-BATTLE.md](../02_Ingame/INGAME-VIEW-AND-BATTLE.md#대전-전용-피벗-모드)를 참고한다.

대전 시작은 콤보·수치 계산·보드 판정과 피벗 설정이 맞는 완주 기록을 상대로 선택한다. 특수 블록을 정의한 규칙은 일반 블록 대전이 도입된 버전 2 기록부터 선택한다. 호환되는 상대가 없으면 첫 기록 도전으로 진입한다. [리플레이 호환 조건](../02_Ingame/INGAME_REPLAY.md#재생-흐름)

상대 탐색 중 동일 Rule/Stage 데이터는 요청 내에서 재사용하며, 캐시된 후보를 선택하면 실제 씬 주입 데이터도 해당 후보로 맞춘다. 상대 상태 UI는 재생 상태·남은 초·점수·피벗 수치가 바뀔 때만 갱신한다. 양쪽 점수와 피벗 수치는 재사용하는 텍스트 버퍼에 정수로 기록해 큰 점수까지 정확히 표시한다.

## 도메인 시스템 개요

`DomainManager`는 열린 탭과 팝업을 스택에 쌓고 `CurrentPath`로 경로를 표시한다. 예를 들어 인벤토리에서 카드 상세 창을 열면 뒤로 가기로 인벤토리에 돌아올 수 있다. [DomainManager.cs](../../02_Scripts/Manager/DomainManager.cs)

## DomainManager API

| 메서드 | 동작 |
|---|---|
| `OpenPopup(name, onOpened?, onFailed?)` | 활성 팝업 컨트롤러가 프리팹을 생성한다. 생성 콜백과 열림 애니메이션 완료 이벤트는 별도다. 실패·취소 시 실패 콜백을 호출한다. |
| `OpenTab(name, callback?)` | 활성 탭 컨트롤러가 전환을 허용하면 기존 탭과 그 위의 팝업을 정리하고 새 탭을 연다. |
| `Back()`, `Close(name)`, `CloseAll()` | 각각 최상위 하나, 지정한 도메인부터 위쪽 전체, 스택 전체를 닫는다. |
| `Peek()`, `IsOpen(name)`, `GetDomain<T>(name)` | 최상위 노드, 열림 여부, 이름으로 찾은 지정 타입 노드를 반환한다. |

## 팝업 생명주기

1. `PopupController.CreatePopup`이 `AssetManager`를 통해 `Popup/{이름}` 프리팹을 로드한다. `DomainManager`가 인스턴스를 스택에 등록하고 `PopupBase.Open`을 호출한다.
2. 열고 닫을 때 딤 배경의 알파와 본문 크기를 보간한다. 안전 영역 프레임이 있는 준비·카드 팝업은 프레임 안에서 Cubic 곡선으로 움직인다. 다른 팝업은 Back 곡선을 사용한다.
3. `PopupHandler.ClosePopup`과 `BackPopup`은 닫기·뒤로 가기를 도메인에 전달한다. 열림 도중 도메인이 닫힘을 요청하면 컨트롤러가 열림 완료 후 닫고 인스턴스를 파괴한다. [PopupBase.cs](../../02_Scripts/Manager/PopupBase.cs) · [PopupController.cs](../../02_Scripts/Manager/PopupController.cs)

## 로비 하단 탭

대전 버튼·디펜스 전장·하단 탭은 같은 기기 안전 영역을 기준으로 배치한다.
로비는 `Lobby/Navigation` 로드가 성공하면 디펜스를 초기화한다. 로드 실패 시 해당 로비에는 재시도 경로가 없어 디펜스와 대전 시작이 막힌다. [로비 초기화](../../02_Scripts/Lobby/LobbyMain.cs)
상점 · 인벤토리 · **메인** · 소셜 · 옵션 순서이며 로비에 들어오면 메인을 표시한다. 다른 네 탭은 메인을 완전히 덮고 하단 바는 계속 표시한다. 메인의 디펜스에서 배치·고용한다. 인벤토리의 [카드 목록](../../03_Prefab/UI/DefenseCardCollection.prefab)을 세로로 스크롤하고 카드를 누르면 [상세 창](../../03_Prefab/UI/Popup/PopupDefenseCard.prefab)에서 개별 강화와 종류별 에너지 변환을 할 수 있다. 탭을 열어 두어도 전투는 진행된다. 대전 준비 팝업은 탭 위에 열린다. [디펜스 화면과 카드 인벤토리](../01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md#콘텐츠와-c-매핑)
버튼을 누르거나 좌우로 스와이프해 이동한다. 세로 드래그는 무시하고 양 끝에서 멈춘다. 메인 선택 또는 도메인 뒤로 가기는 패널을 닫는다. 이동 중에는 패널 콘텐츠 입력을 막는다. 대전 준비를 요청하면 팝업이 닫힐 때까지 메인과 하단 탭의 포인터·키보드 입력을 잠그고, 닫거나 로드에 실패하면 이전 입력을 복원한다. 패널의 콘텐츠와 각 탭의 선택 표시는 [로비 탭 프리팹](../../03_Prefab/UI/LobbyNavigation.prefab)에서 편집한다. [탭 아이콘 원화](../../04_Resources/Images/LobbyTabIcons.png)

<details><summary>내장 ImageGen에 사용한 아이콘 생성 프롬프트</summary>Game asset, a single transparent horizontal sprite atlas for the bottom navigation of a Korean mobile puzzle game. Precisely five separate equal-sized icons in ONE straight row, centered in five equal-width cells, left to right: a tiny shop kiosk with a coral-and-cream striped awning; a golden ochre adventurer backpack with a teal clasp; a small cream house with a seafoam/teal roof and a warm golden doorway; two overlapping coral and turquoise speech bubbles; a blue-silver eight-tooth settings gear with an open circular hole. Cheerful polished enamel toy sculptures, soft bevels, sculpted detail, broad highlights from upper left, crisp legible silhouettes at 44px, near-frontal slight perspective. Coordinated colors and consistent outline weight, same optical size. Wide landscape canvas, generous entirely transparent gutters between each silhouette and around outer edges. Background genuine alpha transparency. Objects floating individually, no cast shadows outside silhouettes, no plates or button tiles, no lettering, no symbols other than the five described, no numbers, no borders, no captions.</details>

## 탭 생명주기

`TabController`는 자신의 `_tabRoot` 안에서 이름이 일치하는 `TabBase`를 찾아 활성화한다. 전환 시작·완료 훅과 `ActivateAnimation`·`DeactivateAnimation`을 재정의할 수 있다. 로비 오버레이는 이 경로에서 가로 전환을 실행한다.

## UIButton (입력 연결)

프리팹 Inspector에서 `_unityButton`, 메시지를 받을 `_root`, 메서드 이름 `_callbackName`을 연결한다. `_callbackValue`를 채우면 클릭할 때 문자열 인자도 함께 전달한다. [UIButton.cs](../../02_Scripts/UI/UIButton.cs)

## 씬별 컨트롤러

| 컨트롤러 | ControllerName | 위치 |
|----------|---------------|------|
| SharedPopupController | `"Shared"` | SharedScene (공용 팝업: 네트워크 에러 등) |
| LobbyPopupController | `"Lobby"` | LobbyScene |
| GamePopupController | `"Game"` | GameScene |
| LobbyNavigation | `"Lobby"` | LobbyNavigationRoot 아래에 생성되는 하단 바와 네 패널 |

## 팝업 추가 방법 (체크리스트)

1. `Assets/03_Prefab/UI/Popup/`에 프리팹을 만들고 같은 객체에 PopupBase와 PopupHandler 서브클래스를 붙인다. PopupBase의 `_dimBackground`, `_contentPanel`을 연결한다. 안전 영역에 맞출 때는 본문 크기의 부모 `_safeFrame`과 터치 높이 기준 `_referenceButton`도 연결한다.
2. 주소 `Popup/{팝업이름}`을 등록하고 UIButton의 `_root`와 `_callbackName`에 핸들러와 메서드명을 지정한다. `DomainManager.Instance.OpenPopup("팝업이름")`으로 연다.

## 사이드 스테이지 팝업

사이드 스테이지 팝업의 기획은 [사이드 스테이지 UI](UI_SIDE_STAGE.md)에 정리한다.

## 탭 추가 방법 (체크리스트)

1. TabController의 `_tabRoot` 아래 객체에 TabBase 서브클래스를 붙이고 `TabName`을 지정하거나 객체 이름을 사용한다.
2. 필요하면 `ActivateAnimation()`과 `DeactivateAnimation()`을 오버라이드하고 `DomainManager.Instance.OpenTab("탭이름")`으로 연다.

## 준비 팝업의 플레이 진입

릴의 세 칸 구성과 생성 프롬프트는 [슬롯 스타일](../00_Guide/ART-STYLE.md#준비-팝업-슬롯), 배경 이미지와 글자색은 [준비 팝업 패널](../00_Guide/ART-STYLE.md#준비-팝업-패널)을 참고한다.

- 로비의 시작 버튼 `Battle 90s`를 누르면 준비 팝업이 열리고 링크·3매치·탭 매치 중 하나를 미리 선택한다. 닫기(×)는 무료다. START를 누르면 릴이 3초간 회전하며 감속한다. 선택된 그림과 모드 이름을 2초간 보여준 뒤 추첨된 Rule로 대전을 준비한다. [릴 이미지](../../04_Resources/Images/ReadyReel.png)
- 라이프는 최초 5개이며 최대 5개까지 30분마다 1개씩 회복한다. 완충 예정 UTC 시각을 PlayerPrefs에 보관해 앱 종료 중에도 충전한다. 대전 준비가 끝나면 라이프 1개를 쓰고 게임 씬으로 이동한다. 잔량이 0이면 START를 비활성화하고 다음 충전까지 남은 시간을 표시한다.
- 실제 출발 순서는 상대 기록·퍼즐 준비, 로비 디펜스의 통행 확인과 저장, 라이프 한 개 차감, 게임 씬 전환이다. 통행 또는 저장을 확인할 수 없으면 팝업에서 다시 시도할 수 있다.
- 대전 결과의 `다시 도전`은 라이프 1개를 사용해 같은 규칙·시드·상대로 새 판을 시작한다. 대전 라이프가 0개면 `로비로`를 누르거나 충전 후 다시 도전할 수 있다. 에디터에서 직접 실행한 스테이지의 시작과 재도전은 무료다.
- 로비와 게임 캔버스는 가로·세로 화면 모두에서 800×1422의 기준 영역을 확보한다. 준비·카드 상세 팝업은 기기 안전 영역의 중앙에 표시하고 딤 배경으로 화면 전체의 뒤쪽 입력을 막는다. 닫기 버튼의 터치 영역은 기준 캔버스에서 132×132이며 두 줄 안내와 라이프 잔량은 영역을 나눠 표시한다. 팝업 버튼은 `PopupReady.prefab`의 `UIButton` 메시지로 연결한다.
- 대전 점수 비교와 상대 상태 표시는 `GameScene.unity`에 연결된 TMP UI이며 상세는 [INGAME-VIEW-AND-BATTLE.md](../02_Ingame/INGAME-VIEW-AND-BATTLE.md#스테이지와-90초-대전)를 참고한다.
