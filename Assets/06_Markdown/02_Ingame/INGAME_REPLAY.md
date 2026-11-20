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

스테이지에서 플레이어가 먼저 클리어하면 해당 틱을 종료 시점으로 저장한다. 결과 패널과 상대 보드는 서로 다른 영역에 표시한다. 결과 화면에 머무르는 동안 상대 보드는 기록을 끝까지 재생하고 종료 틱과 점수를 검증한다. [플레이어와 상대의 고정 틱](../../02_Scripts/PuzzleCore/Controller/PuzzleGameController.cs)

새 대전은 보드 규칙 버전 2를 기록하고 일반 블록만 사용한다. 이전 기록을 직접 재생할 때는 당시의 블록 생성을 복원한다. 버전 필드가 없는 기록은 0으로 읽는다. [리플레이 초기화](../../02_Scripts/PuzzleCore/Controller/ReplayController.cs)

현재 규칙에 특수 블록 정의가 있으면 새 대전은 버전 2 이상 기록에서 상대를 고른다. 일반 블록만 정의한 규칙에서는 버전 1 기록도 후보이며, 탭 매치는 버전 0도 호환된다. 상대 선택과 승패 비교에는 동일한 제한 시간·콤보·수치·피벗 조건을 적용한다. [대전 규칙 비교](../../02_Scripts/PuzzleCore/Module/ReplayData.cs)

---

## ReplayController 배치

- 메인 보드와 독립적으로 동작 (별도 IPuzzleBoard + PuzzleBoardView).
- 상대 보드는 화면 비율에 맞춰 할당된 영역에 축소하고 재생 상태·점수·피벗 수치는 보드 밖에 표시한다. 상대의 발동 테두리와 추가 파괴 표시는 상대 기록의 피벗 상태를 따른다.
- `PuzzleBoardView.skipCameraAlign = true`로 카메라 간섭 방지.

기록 시점의 스테이지 JSON이 있으면 해당 배치를 복원한다. 완료 틱·점수 검증과 60초 기록 호환 조건은 [뷰·리플레이·대전](INGAME-VIEW-AND-BATTLE.md#스테이지와-90초-대전)을 참고한다.
