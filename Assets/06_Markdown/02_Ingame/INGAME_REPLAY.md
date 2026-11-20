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

```
ReplayData 로드 → GameSpec 복원 (동일 Rule/Stage + 동일 시드)
  → IPuzzleBoard 생성 + Initialize
    → PuzzleGameController.FixedUpdate()의 공통 틱에서 ReplayController.AdvanceFrame() 호출
      → 0번 틱부터 Input/InputEnd를 frame과 sequence 순서로 큐에 주입
        → board.FixedUpdate() 후 다음 틱으로 전진
```

---

## ReplayController 배치

- 메인 보드와 독립적으로 동작 (별도 IPuzzleBoard + PuzzleBoardView).
- `viewScale`, `margin` 파라미터로 우측 상단에 축소 자동 배치.
- `PuzzleBoardView.skipCameraAlign = true`로 카메라 간섭 방지.

기록 시점의 스테이지 JSON이 있으면 해당 배치를 복원한다. 완료 틱·점수 검증과 60초 기록 호환 조건은 [뷰·리플레이·대전](INGAME-VIEW-AND-BATTLE.md#스테이지와-90초-대전)을 참고한다.
