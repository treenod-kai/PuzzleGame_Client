# PuzleBattleGame 서버 통신 참고 문서

서버 API 연동, 공유 DTO, 네트워크 레이어 작업 시 참고.

## 콘텐츠와 C# 매핑

[전체 작업 지도](../MAP.md) · [문서 목록](../README.md)

| 찾는 콘텐츠 | 담당 C# 파일 | 주요 메서드·타입·멤버 |
|---|---|---|
| 서버 URL·HTTP 초기화·GET/POST 진입점 | [NetworkManager.cs](../../02_Scripts/Manager/NetworkManager.cs) | `Initialize`, `Get`, `Post` |
| UnityWebRequest·JSON 직렬화·응답/오류 처리 | [HttpNetworkClient.cs](../../02_Scripts/Manager/Network/HttpNetworkClient.cs) | `CoGet`, `CoPost`, `HandleResponse`, `BuildUrl` |
| 유저 데이터 레이어 초기화·로드·초기화 해제 | [UserDataManager.cs](../../02_Scripts/Manager/UserDataManager.cs) | `Initialize`, `LoadIdentity`, `ClearAll` |
| 인증 로그인·계정 연동 | [AuthLayer.cs](../../02_Scripts/Manager/UserData/AuthLayer.cs) | `Login`, `Link` |
| auth/login 게스트 로그인·실패 시 더미 데이터 | [IdentityLayer.cs](../../02_Scripts/Manager/UserData/IdentityLayer.cs) | `Request`, `CreateDummyData`, `Clear` |
| 계정별 디펜스 파일·게스트 진행 이전 | [DefenseProgressStore.cs](../../02_Scripts/Manager/UserData/DefenseProgressStore.cs) | `CurrentOwner`, `Open`, `Save` |

## 시작과 통신

현재 타이틀과 로비는 로그인 없이 시작한다. 대전 상대는 기기에 저장된 퍼즐 리플레이에서 고른다. [게임 흐름](../01_Architecture/ARCHITECTURE_FLOW.md)

서버 연결을 사용하는 호출자가 SharedScene의 매니저를 다음 순서로 초기화한다.

1. `NetworkManager.Initialize(baseUrl)`로 HTTP 클라이언트를 만든다. 인자를 생략할 때의 기본 주소는 `http://localhost:7777`이다.
2. `UserDataManager.Initialize()`로 Identity와 Auth 레이어를 생성한다. 재호출하면 두 인스턴스를 교체한다.
3. 설치 게스트 정보는 `LoadIdentity`, 제공자 로그인은 `Login`, 현재 계정 연동은 `LinkAuthAccount`를 호출한다.

| 진입점 | HTTP 요청 | 결과 |
|---|---|---|
| `LoadIdentity` | `POST auth/login`, 설치 게스트 ID | 서버 유저를 Identity에 담는다. 실패하면 `dummy_user`를 넣고 완료 콜백을 호출한다. 두 경우 모두 `IsLoaded`가 참이므로 다시 요청하려면 `forceRefresh`가 필요하다. |
| `Login` | `POST auth/login`, 제공자 ID와 현재 UID | 인증 결과를 Auth에 저장하고 Identity를 갱신한다. |
| `LinkAuthAccount` | `POST auth/link`, 현재 UID와 제공자 ID | 응답받은 유저를 Identity에 적용한다. |
| `ClearAll` | 로컬 처리 | Identity와 Auth의 메모리 상태를 비운다. 설치 게스트 식별자와 저장 파일은 유지한다. |

`HttpNetworkClient`는 `UnityWebRequest` 코루틴과 UTF-8 JSON을 사용한다. 모델을 추가할 때는 서버 공유 프로젝트에서 `JsonUtility`가 읽을 수 있는 필드명과 타입을 맞추고 DLL을 갱신한다. [공유 DTO 명세](SERVER_DTO.md) · [클라이언트 참조 DLL](../../Plugins/PuzleBattleShared.dll)

## 디펜스의 로컬 계정 저장

- 저장 주체는 현재 유효한 UID, 이 설치에서 확인한 서버 게스트 UID, 설치 게스트 ID 순으로 선택한다.
- 카드·종류별 에너지·골드·배치·전투 진행을 `persistentDataPath/LobbyDefense/<소유자 키의 SHA-256>/progress.json`에 함께 저장한다. 임시 파일을 기록한 뒤 기존 파일을 교체하며 이전 저장본은 `.bak`에 보관한다.
- 해당 설치의 게스트 로그인이 확인되고 UID 폴더가 처음 생성될 때 기존 게스트 폴더를 옮긴다. 이미 진행이 있는 계정의 파일은 유지한다. [보유 카드와 계정](../01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md#유저-데이터와-보유-유닛)

## 계정 UI 연결 전 확인할 문제

- 로그인 진행 중 `ClearAll()`을 호출해도 나중에 도착한 성공 응답이 이전 UID를 복원한다. 로그인 두 건의 응답 순서가 바뀌어도 마지막에 도착한 응답의 계정이 남는다.
- 게스트 로드에 실패한 후 `LinkAuthAccount`를 호출하면 임시 UID `dummy_user`가 연동 요청에 들어간다.
