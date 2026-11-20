# 사이드 스테이지 UI

현재 로비의 `Battle 90s` 버튼은 준비 팝업에서 퍼즐 모드를 추첨한다. 사이드 스테이지의 모드별 목록 팝업은 후속 기획이다. [현재 대전 진입](UI.md#준비-팝업의-플레이-진입)

## 팝업 흐름

1. 로비의 모드 버튼이 `DomainManager.OpenPopup`으로 해당 스테이지 목록을 연다. 제안한 프리팹 이름은 `PopupThreeMatchStage`, `PopupLinkStage`, `PopupTapMatchStage`다.
2. 목록에 잠금·클리어·별 상태를 표시하고 유저가 스테이지를 선택한다. 진행 데이터와 목록 UI는 이 기능을 구현할 때 연결한다.
3. 선택한 ID로 `StageInjection.Instance.MakeGameSpec(ruleAddress, puzzleType, stageId)`를 호출해 성공하면 게임 씬으로 이동한다. [StageInjection.cs](../../02_Scripts/StageMapTool/StageInjection.cs)
