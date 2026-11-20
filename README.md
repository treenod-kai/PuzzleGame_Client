# PuzleBattleGame

Unity URP 퍼즐 배틀 게임입니다. 로비에서는 방어 카드를 배치해 10×10 전장을 지키고, 대전에서는 3매치·링크·탭 매치 중 한 모드로 90초 동안 퍼즐 점수를 겨룹니다.

## 실행

1. [프로젝트 지정 버전](ProjectSettings/ProjectVersion.txt)의 Unity에서 [TitleScene](Assets/01_Scenes/TitleScene.unity)을 열고 Play를 누릅니다. 공용 매니저가 있는 SharedScene이 추가로 로드되고, 타이틀 연출 뒤 로딩 씬을 거쳐 로비에 도착합니다.
2. 로비에서 첫 방어 카드를 배치해 웨이브를 시작합니다. 인벤토리에서 보유 카드를 강화하거나 에너지로 변환할 수 있습니다. [로비 디펜스](Assets/06_Markdown/01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md)
3. `Battle 90s` → `START`를 누르면 퍼즐을 추첨하고 라이프 1개를 사용합니다. 같은 규칙의 호환 기록이 있으면 상대 보드가 그 입력을 재생합니다. 첫 대전은 자신의 기록을 남깁니다. 결과 화면에서 라이프 1개로 재도전하거나 로비로 돌아갈 수 있습니다. [대전·리플레이](Assets/06_Markdown/02_Ingame/INGAME_REPLAY.md)

현재 대전 승패에는 퍼즐 점수가 반영됩니다. 로비 편성을 대전에 가져와 처치 점수까지 합산하는 흐름은 [대전 연동 설계](Assets/06_Markdown/01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md#90초-대전에서-퍼즐-리플레이-활용)에 정리되어 있습니다.

## 점검에서 확인한 문제

- 계정 전환·초기화 뒤에 도착한 로그인 응답이 이전 계정으로 덮어쓸 수 있습니다. 게스트 로그인 실패 후 계정 연동을 요청하면 임시 UID가 전달됩니다. [계정 연동](Assets/06_Markdown/06_Server/SERVER.md#계정-ui-연결-전-확인할-문제)
- 로비 탭 에셋을 불러오지 못하면 디펜스가 초기화되지 않아 해당 로비에서 대전을 시작할 수 없습니다. [로비 초기화](Assets/06_Markdown/04_UI/UI.md#로비-하단-탭)
- 다운로드 스테이지의 중복 좌표가 런타임 검사를 통과합니다. 에디터의 맵 도구는 보드 뷰 연결이 빠져 격자를 표시하지 못합니다. [스테이지 검증](Assets/06_Markdown/03_Data/DATA_STAGE.md) · [맵 도구 상태](Assets/06_Markdown/03_Data/STAGE_MAP_TOOL.md)

## 개발 문서

| 찾는 내용 | 문서 |
|---|---|
| 기능별 담당 코드와 문서 | [작업 지도](Assets/06_Markdown/MAP.md#작업별-빠른-경로) · [문서 인덱스](Assets/06_Markdown/README.md) |
| 퍼즐 보드, 규칙, 스테이지 데이터 | [인게임](Assets/06_Markdown/02_Ingame/INGAME.md) · [데이터](Assets/06_Markdown/03_Data/DATA.md) |
| UI, 안전 영역, 에셋 로딩 | [UI](Assets/06_Markdown/04_UI/UI.md) · [씬·매니저](Assets/06_Markdown/05_Scene/SCENE.md) |
| 작업 규칙과 주요 변경 | [작업 지침](AGENTS.md) · [변경 이력](Assets/06_Markdown/CHANGELOG.md) |
