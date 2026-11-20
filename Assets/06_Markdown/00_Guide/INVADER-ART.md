# 침공 몬스터 방향과 봉쇄 공격

몬스터 5종은 각각 Walk와 Attack이 있다. 동작마다 위·오른쪽 위·오른쪽·오른쪽 아래·아래의 다섯 시점을 그리고, 왼쪽 세 방향은 대응하는 원화를 가로로 반전한다. 전체 구성은 원화 시트 50장, 방향별 클립 80개다. [동작 원본](../../04_Resources/Images/LobbyDefense/Motions) · [5종 움직임 비교](../../../Docs/Previews/LobbyDefense/InvaderDirections.gif)

## 생성 프롬프트

1. [공통 프롬프트](ART-STYLE.md#공통-생성-프롬프트)에 해당 몬스터의 전신 PNG를 첨부한다. 방향별 Attack을 만들 때는 같은 방향의 Walk 첫 프레임도 첨부해 카메라·실루엣·지면 높이를 맞춘다.
2. 아래 시점 표에서 한 줄, 종별 표에서 해당 동작 한 줄을 선택한다. 한 시트 안에서는 시점과 진행 방향을 고정한다. [종별 영문 동작 문구](LOBBY-ART-PROMPTS.md#침공-몬스터의-걸음과-봉쇄-공격)
3. 640×640 투명 시트에 320×320 네 프레임을 왼쪽 위부터 행 순서로 놓는다. Walk와 Attack은 각각 8 FPS다. Walk는 제자리에서 0.5초마다 반복하고, Attack은 준비 → 당기기 → 접촉 → 회복을 한 번 재생한다.

예시 — 조약돌 집게의 오른쪽 위 Attack은 호박색 등껍질이 넓게 보이는 뒤쪽 ¾ 시점이다. 큰 집게를 벌려 몸 쪽으로 당겼다가 세 번째 프레임에서 오른쪽 위로 뻗어 닫고 제자리로 거둔다. 왼쪽 위 클립에서는 큰 집게의 위치도 함께 반전된다. [오른쪽 위 공격 시트](../../04_Resources/Images/LobbyDefense/Motions/PebbleCrab/Directions/PebbleCrab_Attack_NorthEast.png)

| 종 | Walk에서 읽힐 특징 | Attack의 접촉 프레임 |
|---|---|---|
| [물방울 젤리](../../04_Resources/Images/LobbyDefense/Monsters/WaveJelly.png) | 짧은 두 발과 물결 꼬리로 눌렸다 뛰어오른다. | 이마를 앞으로 튕겨 부딪친다. |
| [조약돌 집게](../../04_Resources/Images/LobbyDefense/Monsters/PebbleCrab.png) | 호박색 등껍질 아래에서 다리를 번갈아 내딛는다. | 큰 집게를 앞으로 뻗어 닫는다. |
| [갈대도마뱀](../../04_Resources/Images/LobbyDefense/Monsters/ReedRunner.png) | 대각선의 두 발이 짝을 이루고 긴 꼬리가 균형을 잡는다. | 움츠린 목을 펴고 짧게 문다. |
| [별핵도깨비](../../04_Resources/Images/LobbyDefense/Monsters/ScoreImp.png) | 팔과 반대쪽 발을 번갈아 내민다. 금별은 배 앞면에 있다. | 몸을 비틀어 수정 주먹을 내민다. |
| [길지기 유령](../../04_Resources/Images/LobbyDefense/Monsters/PathWarden.png) | 발 없이 떠서 꼬리를 말고 장갑 낀 손을 번갈아 든다. | 두 손을 모았다가 앞손으로 밀어낸다. |

## Unity에서 재생

| 방향 키 | 보드에서 보는 모습 | 원화와 표시 |
|---|---|---|
| `North` | 위로 멀어진다. 뒤통수·등이 보인다. | 위 원화 |
| `NorthEast` / `NorthWest` | 대각선 위로 멀어진다. 등과 가까운 옆구리가 보인다. | 오른쪽 위 원화 / 같은 원화 반전 |
| `East` / `West` | 머리와 시선이 수평으로 진행한다. | 오른쪽 원화 / 같은 원화 반전 |
| `SouthEast` / `SouthWest` | 대각선 아래로 다가온다. 얼굴과 가슴이 함께 보인다. | 오른쪽 아래 원화 / 같은 원화 반전 |
| `South` | 아래로 다가온다. 두 눈과 배 앞면이 보인다. | 아래 원화 |

클립 주소는 `LobbyDefense/Motions/{몬스터 ID}/{Walk|Attack}/{방향}`이다. 예를 들어 `LobbyDefense/Motions/PebbleCrab/Attack/NorthWest`는 오른쪽 위 시트와 클립의 가로 반전 값을 함께 사용한다. [방향·접촉 프레임 비교](../../../Docs/Previews/LobbyDefense/LobbyDefenseMotion.html#PebbleCrab/Attack/NorthWest)

보드 경로는 상하좌우 네 이웃을 사용한다. 몬스터가 출구로 갈 수 없을 때 인접한 길막 유닛을 향해 공격한다. 세 번째 접촉 그림은 클립 시작 후 0.25초에 보이며, 다음 공격 시각은 전투 모델에서 정한다. [전장과 봉쇄 규칙](../01_Architecture/LOBBY-DEFENSE.md)
