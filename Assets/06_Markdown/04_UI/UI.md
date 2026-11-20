# PuzleBattleGame UI/도메인 시스템 참고 문서

UI, 팝업, 탭 관련 작업 시 이 문서를 참고.

## 콘텐츠와 C# 매핑

[전체 작업 지도](../MAP.md) · [문서 목록](../README.md)

| 찾는 콘텐츠 | 담당 C# 파일 | 주요 메서드·타입·멤버 |
|---|---|---|
| 팝업/탭 스택·뒤로 가기·전체 닫기 | [DomainManager.cs](../../02_Scripts/Manager/DomainManager.cs) | `OpenPopup`, `OpenTab`, `Back`, `CloseAll` |
| 팝업/탭 공통 계약 | [IDomainNode.cs](../../02_Scripts/Manager/IDomainNode.cs) | `IDomainNode` |
| 팝업 생성·파괴·씬별 등록 | [PopupController.cs](../../02_Scripts/Manager/PopupController.cs) | `CreatePopup`, `DestroyPopup` |
| 팝업 열기/닫기 생명주기·애니메이션 | [PopupBase.cs](../../02_Scripts/Manager/PopupBase.cs) | `Open`, `Close`, `OpenRoutine`, `CloseRoutine` |
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
| 하단 다섯 탭·스와이프·모달 입력 | [LobbyNavigation.cs](../../02_Scripts/Lobby/LobbyNavigation.cs) | `SelectTab`, `OnEndDrag`, `SwitchBySwipe` |
| 오버레이 패널의 가로 전환 | [LobbyOverlayTab.cs](../../02_Scripts/Lobby/LobbyOverlayTab.cs) | `Bind`, `ActivateAnimation`, `DeactivateAnimation` |
| 메인 화면의 상주 탭 컴포넌트 | [LobbyTabController.cs](../../02_Scripts/Lobby/LobbyTabController.cs) | `LobbyTabController` |
| 대전 전용 피벗 충전·활성 게이지 | [PivotGaugeView.cs](../../02_Scripts/Battle/Pivot/PivotGaugeView.cs) | `PivotGaugeView`, `SetLayout`, `Refresh`, `Dispose` |

대전의 점수·타이머·피벗 게이지와 퍼즐 안내는 화면의 안전 영역 안에서 보드와 따로 배치한다. 피벗 게이지는 타이머와 같은 폰트를 사용한다. 게임 종료 시 숨기고 컨트롤러 제거 시 UI를 정리한다. 상태 및 설정 규칙은 [INGAME-VIEW-AND-BATTLE.md](../02_Ingame/INGAME-VIEW-AND-BATTLE.md#대전-전용-피벗-모드)를 참고한다.

대전 시작은 콤보·수치 계산·보드 판정과 피벗 설정이 맞는 완주 기록을 상대로 선택한다. 호환되는 상대가 없으면 첫 기록 도전으로 진입한다. 탭 매치는 판정이 같은 이전 보드 규칙의 기록도 상대로 선택한다.

상대 탐색 중 동일 Rule/Stage 데이터는 요청 내에서 재사용하며, 캐시된 후보를 선택하면 실제 씬 주입 데이터도 해당 후보로 맞춘다. 상대 상태 UI는 재생 상태·남은 초·점수·피벗 수치가 바뀔 때만 갱신한다. 양쪽 점수와 피벗 수치는 재사용하는 텍스트 버퍼에 정수로 기록해 큰 점수까지 정확히 표시한다.

## 도메인 시스템 개요

URL 방식 경로로 UI 상태를 추적 (예: `/Lobby/Shop/ItemDetail`).

```
DomainManager (SharedScene 싱글톤)
├─ 팝업: 스택 방식 (생성/파괴)
│   PopupController → PopupBase (프리팹) → PopupHandler (로직)
└─ 탭: 전환 방식 (활성/비활성)
    TabController → TabBase (UI)
```

## DomainManager API
### 열기
| 메서드 | 동작 |
|--------|------|
| `OpenPopup(name, onOpened?, onFailed?)` | 프리팹 생성 후 스택에 등록하고 연다. 로드 실패나 요청 취소 시 onFailed 호출 |
| `OpenTab(name, callback?)` | 활성 컨트롤러의 CanOpenTab 확인 → 기존 탭과 위쪽 팝업 정리 → 새 탭 활성화 |

### 닫기
| 메서드 | 동작 |
|--------|------|
| `Close(name)` | 해당 도메인 + 그 위의 모든 도메인 닫기 |
| `Back()` | 최상위 도메인 하나 닫기 |
| `CloseAll()` | 전체 도메인 닫기 (씬 전환 시 Main에서 호출) |

### 조회
| 메서드 | 반환 |
|--------|------|
| `Peek()` | 최상위 IDomainNode |
| `IsOpen(name)` | bool |
| `GetDomain<T>(name)` | 타입 캐스팅된 도메인 인스턴스 |
| `CurrentPath` | URL 문자열 (예: "/Lobby/Shop") |

### 컨트롤러 등록
- `Register(PopupController)` / `Unregister(PopupController)`
- `Register(TabController)` / `Unregister(TabController)`
- OnEnable/OnDisable에서 자동 호출됨

## 팝업 생명주기

```
DomainManager.OpenPopup("ItemDetail")
  → _activePopupController.CreatePopup("ItemDetail")
    → AssetManager.LoadAssetAsync<GameObject>로 "Popup/ItemDetail" 주소 로드
      → Instantiate → PopupBase 컴포넌트 확보
        → _domainStack에 추가
          → PopupBase.Open()
            → dim 페이드인 + content 스케일 (0→1, OutBack, 0.3초)
              → OnOpened 이벤트
```

```
PopupHandler.ClosePopup() 또는 DomainManager.Close(name)
  → PopupController.DestroyPopup() → PopupBase.Close()
    → content 스케일 (1→0, InBack) + dim 페이드아웃
      → OnClosed 이벤트 → GameObject 파괴
```

### PopupBase 주요 멤버
| 멤버 | 용도 |
|------|------|
| `PopupName` / `DomainName` | 식별 이름 |
| `IsAnimating` | 애니메이션 중 여부 (true일 때 조작 차단) |
| `_dimBackground` (Image) | 딤 배경 (alpha 0→0.5) |
| `_contentPanel` (RectTransform) | 콘텐츠 패널 (스케일 애니메이션) |
| `_animationDuration` | 기본 0.3초 |
| `Open()` / `Close(callback?)` | 열기/닫기 애니메이션 |
| `RequestClose()` | `DomainManager.Close(PopupName)` 호출 |
| `RequestBack()` | `DomainManager.Back()` 호출 |

## 팝업 핸들러 (PopupHandler)

각 팝업은 PopupBase와 같은 객체에 PopupHandler 서브클래스를 둔다.

- `ClosePopup()`: `DomainManager.Close(PopupName)` (IsAnimating 중이면 차단)
- `BackPopup()`: `DomainManager.Back()` (IsAnimating 중이면 차단)
- 메서드는 `UIButton`의 `SendMessage`로 호출됨

## 로비 하단 탭

상점 · 인벤토리 · **메인** · 소셜 · 옵션 순서이며 로비에 들어오면 메인을 표시한다. `MainTab`과 방치 화면은 씬에 계속 남는다. 나머지 네 탭은 별도 UI 레이어에 열리고, 대전 준비 팝업은 그 위에 열린다. 탭 이동은 라이프를 사용하지 않는다.
버튼을 누르거나 좌우로 스와이프해 이동한다. 세로 드래그는 무시하고 양 끝에서 멈춘다. 메인 선택 또는 도메인 뒤로 가기는 패널을 닫는다. 이동 중에는 패널 콘텐츠 입력을 막는다. 대전 준비를 요청하면 팝업이 닫힐 때까지 메인과 하단 탭의 포인터·키보드 입력을 잠그고, 닫거나 로드에 실패하면 이전 입력을 복원한다. 패널의 콘텐츠와 각 탭의 선택 표시는 [로비 탭 프리팹](../../03_Prefab/UI/LobbyNavigation.prefab)에서 편집한다. [탭 아이콘 원화](../../04_Resources/Images/LobbyTabIcons.png)

<details><summary>내장 ImageGen에 사용한 아이콘 생성 프롬프트</summary>Game asset, a single transparent horizontal sprite atlas for the bottom navigation of a Korean mobile puzzle game. Precisely five separate equal-sized icons in ONE straight row, centered in five equal-width cells, left to right: a tiny shop kiosk with a coral-and-cream striped awning; a golden ochre adventurer backpack with a teal clasp; a small cream house with a seafoam/teal roof and a warm golden doorway; two overlapping coral and turquoise speech bubbles; a blue-silver eight-tooth settings gear with an open circular hole. Cheerful polished enamel toy sculptures, soft bevels, sculpted detail, broad highlights from upper left, crisp legible silhouettes at 44px, near-frontal slight perspective. Coordinated colors and consistent outline weight, same optical size. Wide landscape canvas, generous entirely transparent gutters between each silhouette and around outer edges. Background genuine alpha transparency. Objects floating individually, no cast shadows outside silhouettes, no plates or button tiles, no lettering, no symbols other than the five described, no numbers, no borders, no captions.</details>

## 탭 생명주기

```
DomainManager.OpenTab("ShopTab")
  → 이전 탭 위의 팝업 전부 Close
    → 이전 탭 Deactivate
      → _activeTabController.ActivateTab("ShopTab")
        → _tabRoot 하위에서 TabBase 검색 (비활성 포함)
          → TabBase.Activate()
            → OnActivateStart() → ActivateAnimation() → OnActivateComplete()
              → OnActivated 이벤트
```

### TabBase 주요 멤버
| 멤버 | 용도 |
|------|------|
| `TabName` / `DomainName` | 식별 이름 (기본값: GameObject.name) |
| `IsAnimating` | 애니메이션 중 여부 |
| `Activate()` | 활성화 (SetActive(true) + 애니메이션) |
| `Deactivate(callback?)` | 비활성화 (애니메이션 → SetActive(false)) |

### 오버라이드 포인트 (protected virtual)
```csharp
OnActivateStart()       // 활성화 시작 전
ActivateAnimation()     // IEnumerator - 커스텀 애니메이션
OnActivateComplete()    // 활성화 완료 후
OnDeactivateStart()     // 비활성화 시작 전
DeactivateAnimation()   // IEnumerator - 커스텀 애니메이션
OnDeactivateComplete()  // 비활성화 완료 후
```

## UIButton (입력 연결)

Inspector에서 설정하는 범용 버튼 컴포넌트.

| 필드 | 용도 |
|------|------|
| `_root` (MonoBehaviour) | 메시지를 받을 대상 (PopupHandler, TabBase 등) |
| `_unityButton` (Button) | Unity Button 컴포넌트 |
| `_callbackName` (string) | 호출할 메서드 이름 |
| `_callbackValue` (string) | 선택적 문자열 인자 |

**동작**: `Button.onClick` → `UIButton.OnClickEvent()` → `_root.SendMessage(_callbackName, _callbackValue?)`

## 씬별 컨트롤러

| 컨트롤러 | ControllerName | 위치 |
|----------|---------------|------|
| SharedPopupController | `"Shared"` | SharedScene (공용 팝업: 네트워크 에러 등) |
| LobbyPopupController | `"Lobby"` | LobbyScene |
| GamePopupController | `"Game"` | GameScene |
| LobbyNavigation | `"Lobby"` | LobbyNavigationRoot 아래에 생성되는 하단 바와 네 패널 |

## 팝업 추가 방법 (체크리스트)

1. `Assets/03_Prefab/UI/Popup/`에 프리팹을 만들고 같은 객체에 PopupBase와 PopupHandler 서브클래스를 붙인다. PopupBase의 `_dimBackground`, `_contentPanel`을 연결한다.
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
- 대전 준비 시 추첨된 Rule과 점수·보드 판정·피벗 설정이 맞는 최근 90초 완주 기록을 상대로 선택해 게임 씬에서 자동 재생한다. 호환 기록이 없으면 혼자 첫 대전 기록에 도전한다.
- 대전 결과의 `다시 도전`은 라이프 1개를 사용해 같은 규칙·시드·상대로 새 판을 시작한다. 대전 라이프가 0개면 `로비로`를 누르거나 충전 후 다시 도전할 수 있다. 에디터에서 직접 실행한 스테이지의 시작과 재도전은 무료다.
- 로비와 게임 캔버스는 가로·세로 화면 모두에서 800×1422의 기준 영역을 확보한다. 준비 팝업은 중앙에 표시하고 딤 배경으로 화면 전체의 뒤쪽 입력을 막는다. 팝업 버튼은 `PopupReady.prefab`의 `UIButton` 메시지로 연결한다.
- 대전 점수 비교와 상대 상태 표시는 `GameScene.unity`에 연결된 TMP UI이며 상세는 [INGAME-VIEW-AND-BATTLE.md](../02_Ingame/INGAME-VIEW-AND-BATTLE.md#스테이지와-90초-대전)를 참고한다.
