# PuzleBattleGame 씬/매니저/인프라 참고 문서

## 콘텐츠와 C# 매핑

[전체 작업 지도](../MAP.md) · [문서 목록](../README.md)

| 찾는 콘텐츠 | 담당 C# 파일 | 주요 메서드·타입·멤버 |
|---|---|---|
| SharedScene 자동 로드·중복 정리·씬 전환 | [Main.cs](../../02_Scripts/Manager/Main.cs) | `SceneEnum`, `AutoCreate`, `MoveScene`, `CoMoveScene`, `IsMovingScene` |
| 타이틀 CI 연출·로비 진입 | [TitleMain.cs](../../02_Scripts/Title/TitleMain.cs) | `Start` |
| 90초 대전 준비 팝업·상대 기록 선택 | [LobbyMain.cs](../../02_Scripts/Lobby/LobbyMain.cs) | `OnClickBattle`, `PrepareBattle` |
| 에셋 로딩 요청 공유·캐시·해제 | [AssetManager.cs](../../02_Scripts/Manager/AssetManager.cs) | `AssetArguments`, `LoadAsset`, `LoadAssetAsync`, `MarkPersistent`, `ReleaseAll` |
| 오브젝트 풀 대여·반환·씬 전환 시 정리 | [PoolManager.cs](../../02_Scripts/Manager/PoolManager.cs) | `Get`, `Release`, `ClearIfCreated`, `Clear` |
| 메인 카메라 접근·중복 처리 | [CameraController.cs](../../02_Scripts/CameraController.cs) | `MainCamera` |
| 사운드 매니저 틀 (스텁) | [SoundManager.cs](../../02_Scripts/Manager/SoundManager.cs) | `Awake` |
| 게임 데이터 매니저 틀 (스텁; 실제 사양 로드는 StageInjection) | [GameDataManager.cs](../../02_Scripts/Manager/GameDataManager.cs) | `Awake` |
| 공통 UI 매니저 틀 (스텁; 팝업/탭은 DomainManager) | [UIManager.cs](../../02_Scripts/Manager/UIManager.cs) | `Awake` |
| 다국어 매니저 틀 (스텁) | [LocalizationManager.cs](../../02_Scripts/Manager/LocalizationManager.cs) | `Awake` |

게임 씬의 Controller는 [INGAME.md](../02_Ingame/INGAME.md), 팝업/탭 매니저는 [UI.md](../04_UI/UI.md), 네트워크/유저 데이터 매니저는 [SERVER.md](../06_Server/SERVER.md)에 매핑되어 있습니다.

## 씬 구성과 수명

[빌드 씬 목록](../../../ProjectSettings/EditorBuildSettings.asset)은 Title, Shared, Loading, Lobby, Game 순서다. 맵 편집용 ToolScene은 에디터에서 직접 연다. [스테이지 맵 툴](../03_Data/DATA_STAGE.md)

| 씬 | 실행 중 역할 |
|---|---|
| TitleScene | CI 연출 후 로비로 전환한다. 전체 흐름을 확인할 때 이 씬에서 시작한다. |
| SharedScene | 첫 씬 로드 뒤 `Main.AutoCreate`가 추가하고 활성 씬으로 지정한다. 공용 Canvas·팝업 루트, EventSystem, ManagerRoot를 유지한다. |
| LoadingScene | 콘텐츠 씬을 교체하는 동안 표시한다. |
| LobbyScene | 방어 규칙·계정별 카드를 불러오고 대전 준비 팝업에서 상대 기록을 선택한다. |
| GameScene | 본인 퍼즐과 상대 리플레이를 진행하고 결과 화면에서 재도전 또는 로비 복귀를 처리한다. |

`Main`과 메인 `CameraController`는 직접 `DontDestroyOnLoad`를 호출한다. 네트워크·유저 데이터·스텁 매니저는 `Awake`에서 싱글톤을 등록하며 상주하는 SharedScene이 수명을 보장한다. 콘텐츠 씬에 중복 배치한 EventSystem·입력 모듈·AudioListener는 `Main`이 정리한다. 정리 전에 EventSystem 두 개가 활성화되면 로드 중 중복 경고가 출력된다.

## 씬 전환

1. `Main.MoveScene(pre, next)`가 중복 요청을 막고 `DomainManager.CloseAll()`로 팝업과 탭을 닫는다.
2. LoadingScene을 추가 로드하고 이전 콘텐츠 씬을 언로드한다.
3. `PoolManager.ClearIfCreated()`로 대기 객체를 정리한 뒤 `AssetManager.ReleaseAll()`로 비영구 에셋을 해제한다.
4. 다음 씬의 로드 진행률이 0.9에 도달하면 활성화를 허용한다. LoadingScene 언로드가 끝나면 전환 잠금을 푼다.

타이틀·대전 입장·재도전 순서는 [게임 흐름](../01_Architecture/ARCHITECTURE_FLOW.md)에서 확인할 수 있다.

## 에셋과 풀

| API | 수명과 실패 처리 |
|---|---|
| `LoadAsset<T>`, `LoadAssetAsync<T>` | 같은 주소의 동기·비동기 요청이 핸들을 공유한다. 타입이 다르면 오류를 전달하고 실패한 요청은 다시 시도할 수 있다. |
| `LoadGameObject`, `LoadGameObjectAsync` | 로드한 프리팹을 지정한 부모 아래에 생성한다. |
| `MarkPersistent`, `ReleaseAll` | 영구 주소는 전환 후에도 유지한다. 진행 중인 비영구 요청은 해제하고 실패 콜백을 호출한다. |
| `PoolManager.Get`, `Release` | 프리팹별로 대여·반환한다. 파괴된 대기 인스턴스는 건너뛰고 중복 반환은 무시한다. |

Addressables 에셋은 `AssetManager`에서 로드한다. 대표 주소는 `Lobby/Navigation`, `Popup/PopupReady`, `LobbyDefense/Rules`, 기본 대전 맵 `Stage`다.
번호형 스테이지 키는 `StageStorage`가 다운로드 파일을 우선하고 파일이 없으면 Resources에서 읽는다. 리플레이에 맵 스냅샷이 있으면 그 JSON을 먼저 적용한다. [스테이지 로드](../03_Data/DATA_STAGE.md)

## 새 씬·매니저 추가

- 플레이어에서 전환할 씬은 `Assets/01_Scenes/`에 만들고 `SceneEnum`과 빌드 씬 목록에 등록한다.
- 상주 매니저는 SharedScene의 ManagerRoot에 배치하고 중복 인스턴스 처리를 확인한다. 팝업과 탭은 [도메인 시스템](../04_UI/UI.md)으로 관리한다.
