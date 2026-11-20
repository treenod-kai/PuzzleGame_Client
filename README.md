# PuzleBattleGame

3매치·링크·탭 매치를 지원하는 Unity URP 퍼즐 게임입니다. 로비에서 로컬 리플레이를 상대로 90초 점수 대전을 시작합니다. 호환되는 대전 기록이 없으면 첫 기록에 도전합니다.

## 실행 흐름

1. 프로젝트에 지정된 Unity 에디터로 열고 `TitleScene`에서 Play를 누릅니다. 에디터 버전은 [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), URP·Addressables·Input System 버전은 [manifest.json](Packages/manifest.json)에 있습니다.
2. 타이틀이 표시될 때 `Main`이 공통 매니저와 카메라가 있는 `SharedScene`을 추가로 로드합니다. 타이틀 연출이 끝나면 로딩 씬을 거쳐 로비로 이동합니다.
3. 로비에서 `Battle 90s`를 누르고 준비 팝업에서 `START`를 누릅니다. 슬롯이 링크·3매치·탭 매치 중 하나에 멈추고 대전 준비가 끝나면 라이프 1개를 사용합니다. 호환 기록이 있으면 상대 보드에서 자동으로 재생합니다. 대전이 끝나면 기록을 로컬에 저장합니다. 결과 화면에서 라이프 1개로 같은 조건에 다시 도전하거나 로비로 돌아갈 수 있습니다.

## 코드 읽기

| 궁금한 점 | 시작할 곳 |
|---|---|
| 어떤 보드와 블럭이 생성되는가? | [Rule JSON](Assets/05_Table/Rule)과 [Stage JSON](Assets/05_Table/Stage)을 [StageInjection](Assets/02_Scripts/StageMapTool/StageInjection.cs)이 읽어 `GameSpec`으로 조립합니다. |
| 한 번의 입력이 화면에 어떻게 반영되는가? | [PuzzleGameController](Assets/02_Scripts/PuzzleCore/Controller/PuzzleGameController.cs)가 입력을 수집하고 고정 틱마다 보드를 진행합니다. [PuzzleBoardView](Assets/02_Scripts/PuzzleCore/View/PuzzleBoardView.cs)가 보드의 액션 큐를 읽어 이동·생성·파괴를 그립니다. |
| 대전 상대와 점수는 어떻게 결정되는가? | [LobbyMain](Assets/02_Scripts/Lobby/LobbyMain.cs)이 호환되는 기록을 고르고 [ReplayController](Assets/02_Scripts/PuzzleCore/Controller/ReplayController.cs)가 기록된 시드와 입력으로 상대 보드를 재생합니다. |

## 씬 구성

`SharedScene(상주)` → `TitleScene` → `LoadingScene` → `LobbyScene` → `GameScene`

## 문서

상세 문서는 [`Assets/06_Markdown/`](Assets/06_Markdown/README.md) 폴더 참고. 콘텐츠별 폴더로 묶여 있으며, 작업 영역별 200자 요약 + 링크로 정리되어 있습니다.

아웃게임의 배치·보상·대전 연결은 [로비 디펜스 기획서](Assets/06_Markdown/01_Architecture/LOBBY-DEFENSE.md), 원화·Unity 임포트 기준은 [이미지 생성 가이드](Assets/06_Markdown/00_Guide/ART-STYLE.md)를 참고하세요.

기능별 파일과 주요 메서드는 [작업별 코드 지도](Assets/06_Markdown/MAP.md#작업별-빠른-경로), 구현 범위와 설계 방향은 [아키텍처](Assets/06_Markdown/01_Architecture/ARCHITECTURE.md)에서 확인할 수 있습니다.

## 향후 계획

- 적(Enemy) AI 및 배틀 연동
- 줄 제거 등 특수 블럭 확장
