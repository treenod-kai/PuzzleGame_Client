# PuzleBattleGame 리플레이 상세

[인게임 구조](INGAME.md) · [리플레이 JSON 형식](../03_Data/DATA_SCHEMA.md#replaydata-json-구조)

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

스테이지는 목표 달성 또는 시간 만료 틱을 기록한다. 현재 대전에서는 양쪽 보드가 4,500틱을 진행한다. 재생 종료 틱과 최종 점수가 기록과 일치해야 승패를 비교한다. [플레이어와 상대의 고정 틱](../../02_Scripts/PuzzleCore/Controller/PuzzleGameController.cs)

새 기록의 보드 규칙은 버전 4다. 각 기록은 저장할 당시까지 도입된 규칙으로 재생한다. [리플레이 초기화](../../02_Scripts/PuzzleCore/Controller/ReplayController.cs)

| 버전 | 해당 버전에서 도입된 규칙 |
|---|---|
| 0 | 버전 필드가 없는 기록의 기본값. 초기 링크 배치와 마지막 틱 매칭 판정을 복원한다. |
| 1 | 링크 재배치 및 마지막 틱의 매칭 점수 처리 변경. |
| 2 | 대전의 고정 배치와 생성기에서 일반 블록만 사용한다. |
| 3 | 3매치는 손을 뗀 틱에 교환한다. 이전 기록은 인접 칸을 입력한 틱에 교환한다. 입력 종료의 `canceled`는 시스템 취소를 나타내며, 필드가 없으면 정상 해제다. |
| 4 | 3매치 스테이지의 초기 배치와 리필에서 원형 폭탄을 제외한다. 버전 0~3 스테이지 기록은 원래 폭탄 배치를 재현한다. |

현재 규칙에 특수 블록 정의가 있으면 새 대전은 버전 2 이상 기록에서 상대를 고른다. 일반 블록만 정의한 규칙에서는 버전 1 기록도 후보이며, 탭 매치는 버전 0도 호환된다. 상대 선택과 승패 비교에는 동일한 제한 시간·콤보·수치·피벗 조건을 적용한다. [대전 규칙 비교](../../02_Scripts/PuzzleCore/Module/ReplayData.cs)

---

## ReplayController 배치

- 메인 보드와 독립적으로 동작 (별도 IPuzzleBoard + PuzzleBoardView).
- 상대 보드는 화면 비율에 맞춰 할당된 영역에 축소하고 재생 상태·점수·피벗 수치는 보드 밖에 표시한다. 상대의 발동 테두리와 추가 파괴 표시는 상대 기록의 피벗 상태를 따른다.
- `PuzzleBoardView.skipCameraAlign = true`로 카메라 간섭 방지.

기록 시점의 스테이지 JSON이 있으면 해당 배치를 복원한다. 예전 60초 기록은 3,000틱으로 재생한다. [대전 기록 호환성](../03_Data/DATA_SCHEMA.md#대전-기록-호환성)
