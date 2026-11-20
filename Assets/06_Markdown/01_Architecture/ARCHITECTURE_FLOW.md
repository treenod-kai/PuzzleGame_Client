# PuzleBattleGame 게임 흐름 상세

`ARCHITECTURE.md`의 게임 흐름 분리 문서.

---

## 현재 로비 진입과 게임 종료

```
[앱 시작]
  TitleScene 시작 → Main.AutoCreate()가 SharedScene 추가 로드
    → TitleMain.Start()에서 CI 연출
      → LoadingScene 경유
        → LobbyScene 메인 탭 → [Battle 90s] → 대전 준비 팝업
          → START → 슬롯 정지 → 같은 Rule의 호환 대전 기록 확인
            → 대전 준비 성공 → 라이프 1개 사용 → LoadingScene → GameScene
              → Board 생성 + Initialize + View 그리기 → 게임 루프
              → 상대 기록이 있으면 ReplayController 초기화 → 우측 상단 자동 재생
                → 90초 종료 → 기록 저장 → 같은 조건으로 재도전(라이프 1개) 또는 로비 이동
```

로비의 상점·인벤토리·소셜·옵션은 방치 메인 위에서 좌우로 전환되는 UI 패널이다. 메인 복귀와 탭 이동은 무료다. [로비 하단 탭](../04_UI/UI.md#로비-하단-탭)

## 사이드 스테이지형 콘텐츠 (기획)

```
[사이드 콘텐츠 진입]
  LobbyScene 사이드 콘텐츠 버튼
    → 모드별 스테이지 전체 팝업
      → 스테이지 번호 선택
        → StageStorage로 Resources 또는 다운로드 StageData 로드
        → StageInjection.MakeGameSpec(ruleAddress, puzzleType, stageId)
          → GameScene 또는 전용 스테이지 씬에서 플레이
```

씬 전환 상세는 `SCENE.md` 참고.
