# PuzleBattleGame 리플레이 상세

`INGAME.md`의 리플레이 시스템 분리 문서.

---

## 기록 흐름

```
게임 시작 → StageInjection이 randomSeed 생성 → Board.Initialize(spec)에 시드 주입
게임 중   → Board가 InputRecord / InputEndRecord 자동 기록
게임 종료 → PuzzleGameController가 Finish 감지
  → board.GetRecordedInputs() + GetRecordedInputEnds()
    → ReplayData 조립 → ReplayStorage.Save() → JSON 파일 저장
```

---

## 재생 흐름

대전 준비 팝업에서 모드가 결정되면 해당 Rule의 90초 완주 기록을 최신순으로 확인한다. 규칙·스테이지가 유효하고 현재 판정과 호환되는 첫 기록을 상대 보드에 전달한다. 호환 기록이 없으면 혼자 첫 대전 기록에 도전한다. [대전 상대 선택](../../02_Scripts/Lobby/LobbyMain.cs)

```
ReplayData 로드 → GameSpec 복원 (동일 Rule/Stage + 동일 시드)
  → IPuzzleBoard 생성 + Initialize
    → PuzzleGameController.FixedUpdate()의 공통 틱에서 ReplayController.AdvanceFrame() 호출
      → 0번 틱부터 Input/InputEnd를 frame과 sequence 순서로 큐에 주입
        → board.FixedUpdate() 후 다음 틱으로 전진
```

스테이지에서 플레이어가 먼저 클리어하면 해당 틱을 종료 시점으로 저장한다. 결과 패널은 화면 하단에 표시해 우측 상단의 상대 보드를 볼 수 있다. 결과 화면에 머무르는 동안 상대 보드는 기록을 끝까지 재생하고 종료 틱과 점수를 검증한다. [플레이어와 상대의 고정 틱](../../02_Scripts/PuzzleCore/Controller/PuzzleGameController.cs)

기록에 `boardRuleVersion`이 없으면 버전 0의 보드 규칙으로 상대를 재생한다. 링크와 3매치 대전은 현재 보드 규칙 버전이 일치하는 상대만 선택한다. 탭 매치는 버전 0과 현재 버전의 점수 판정이 같아 제한 시간·콤보·수치·피벗 규칙을 만족하면 이전 기록과도 대전할 수 있다. [리플레이 초기화](../../02_Scripts/PuzzleCore/Controller/ReplayController.cs)

---

## ReplayController 배치

- 메인 보드와 독립적으로 동작 (별도 IPuzzleBoard + PuzzleBoardView).
- `viewScale`, `margin` 파라미터로 우측 상단에 축소 자동 배치.
- `PuzzleBoardView.skipCameraAlign = true`로 카메라 간섭 방지.

기록 시점의 스테이지 JSON이 있으면 해당 배치를 복원한다. 완료 틱·점수 검증과 60초 기록 호환 조건은 [뷰·리플레이·대전](INGAME-VIEW-AND-BATTLE.md#스테이지와-90초-대전)을 참고한다.
