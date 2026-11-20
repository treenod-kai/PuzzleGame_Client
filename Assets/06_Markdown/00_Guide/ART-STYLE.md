# 게임 이미지 스타일 가이드

새 그림을 생성할 때는 같은 용도의 게임 PNG를 참조로 첨부하고 아래 공통 프롬프트를 사용한다. 기존 캐릭터의 프로필·동작·투사체는 항상 그 캐릭터의 전신 PNG에서 출발한다.

## 기준 원화 고르기

| 만들 그림 | 첨부할 원본 | 원본에서 유지할 특징 |
|---|---|---|
| 보드 블럭 | [빨간 별 젤리](../../04_Resources/Ingame/Block/Block_100-1.png) | 약간 불규칙한 둥근 몸, 안으로 파인 문양, 왼쪽 위의 넓은 타원 반사광, 짙은 아래 테두리 |
| 타일·패널 | [청록 타일](../../04_Resources/Images/LagoonSquare.png) · [레몬색 패널](../../04_Resources/Images/ReadyPanelYellow.png) | 두껍고 둥근 에나멜 테두리, 그림이나 글자가 올라가는 차분한 중앙 |
| 새 수비 유닛 | [진주 수비병](../../04_Resources/Images/LobbyDefense/Defenders/PearlSentry.png) | 큰 머리와 짧은 몸, 눈썹과 눈으로 읽히는 표정, 청록·아이보리 포인트, 역할을 알려 주는 장비 한 가지 |
| 새 침공 몬스터 | [물방울 젤리](../../04_Resources/Images/LobbyDefense/Monsters/WaveJelly.png) | 옆에서 읽히는 얼굴, 큰 몸통과 특징적인 꼬리·등껍질, 발과 몸통 사이의 명암 |
| 프로필·동작 | 대상의 전신 + 같은 종류의 [프로필](../../04_Resources/Images/LobbyDefense/Profiles/PearlSentry.png) 또는 [공격 시트](../../04_Resources/Images/LobbyDefense/Motions/PearlSentry/PearlSentry_Attack.png) | 첫 그림에서 얼굴·색·장비를, 둘째 그림에서 크롭·발 기준선·프레임 배치를 가져온다. |
| 투사체 | 발사하는 유닛의 전신 + [진주 방울](../../04_Resources/Images/LobbyDefense/Projectiles/PearlSentry/Attack.png) | 장비와 같은 재질·색을 쓰고 작은 크기에서 진행 방향이 읽히게 한다. |

## 공통 생성 프롬프트

아래 문장을 복사한 뒤 대괄호를 채운다. 같은 세트를 만들 때는 참조 원본과 첫 세 줄을 유지하고 대상·구도·납품 규격만 바꾼다.

```text
첨부한 게임 원화와 한 세트로 쓰는 모바일 퍼즐 게임용 2D 원화를 그린다.
큰 형태 두세 개로 실루엣을 잡고, 주된 볼륨 왼쪽 위에 넓고 부드러운 밝은 반사광을 둔다.
아래와 형태가 겹치는 곳은 본체색보다 짙게 칠한다. 얼굴·문양·장비는 작게 보아도 구별되게 한다.
대상은 [ID와 용도]이며 [구별되는 색, 형태, 문양 또는 장비 한 가지]가 특징이다.
[참조 원본에서 유지할 얼굴·비율·재질]을 유지한다. 이번에 바꿀 요소는 [포즈·시점·새 문양]이다.
[프레임별 동작 또는 정지 구도]를 [방향·배치·여백]에 맞춰 그린다.
최종 규격은 [가로×세로, 프레임 수, 배경 처리]다. 글자·숫자·워터마크는 생략한다.
```

**작성 예시 — 진주 수비병의 Attack.** 전신을 첫 참조로, 기존 동작 시트를 배치 참조로 첨부한다. 공통 문장 뒤에 다음 지시를 붙인다.

> 크림색 수달의 눈썹, 청록 망토, 금띠가 달린 파란 진주 지팡이를 유지한다. 네 칸 모두 왼쪽을 본다. ① 지팡이를 가슴에 안고 선다. ② 두 앞발로 뒤로 당긴다. ③ 진주 끝을 왼쪽으로 내밀고 작은 섬광을 낸다. ④ 가슴 앞으로 거둔다. 투명한 640×640 시트에 2×2로 배치하고 몸 크기와 발 기준선을 고정한다.

각 유닛의 색·장비·공격·장기자랑과 투사체 13종에 사용한 개별 문구는 [로비 디펜스 개별 생성 문구](LOBBY-ART-PROMPTS.md)에 있다.

## 보드 블럭

256×256 투명 PNG, 단일 Sprite, 중앙 피벗, PPU 1로 납품한다. 32px로 줄였을 때 색과 가운데 문양으로 종류를 구별할 수 있어야 한다. [일반 블럭](../../04_Resources/Ingame/Block/Block_100-1.png) · [폭탄 블럭](../../04_Resources/Ingame/Block/Block_200-1.png)

폭탄용 추가 문구 예시 — 자두색 젤리 구체에 금빛 네 갈래 불꽃 하나를 넣고 오른쪽 위에는 짧은 심지를 단다. 일반 블럭과 몸 크기를 맞추고 심지 끝까지 투명 여백 안에 넣는다. Addressables 주소 예시는 `Block_200-1`이다. [블럭 등록 절차](../03_Data/RULES-AND-STAGES.md#새-블럭-추가-방법)

## 보드 타일과 피벗 게이지

- 타일은 짙은 청록 에나멜의 오목한 칸이다. 왼쪽 위 테두리를 밝히고 중앙의 대비를 낮춰 블럭 문양이 보이게 한다. 주소는 `squareCell`과 `hexagonCell`이다. [사각 타일](../../04_Resources/Images/LagoonSquare.png) · [육각 타일](../../04_Resources/Images/LagoonHex.png)
- 게이지는 금색 별이 달린 청록색 외곽과 색을 입힐 수 있는 진주색 채움을 각각 그린다. 글자·눈금은 생략한다. 주소는 `PivotGaugeFrame`과 `PivotGaugeFill`이다. [프레임](../../04_Resources/Images/PivotGaugeFrame.png) · [채움](../../04_Resources/Images/PivotGaugeFill.png)

## 준비 팝업 슬롯

릴은 2172×724 텍스처 한 장이다. 같은 너비의 카드 세 장을 왼쪽부터 링크 → 3매치 → 탭 매치 순으로 붙인다. 민트 바탕에 파란 젤리를 선으로 연결하고, 분홍 바탕에 빨간 별 젤리를 정렬하고, 살구 바탕에 손끝으로 주황 젤리를 누르는 순간을 그린다. 카드를 한 칸씩 잘랐을 때도 동작이 읽혀야 한다. `RawImage`의 가로 반복을 위해 Wrap U를 Repeat로 유지한다. [릴 원본](../../04_Resources/Images/ReadyReel.png) · [준비 팝업 동작](../04_UI/UI.md#준비-팝업의-플레이-진입)

## 준비 팝업 패널

팝업은 연한 버터색 중앙과 두꺼운 레몬·금색 테두리를 사용한다. 반사광과 장식은 가장자리에 두고 제목·릴·버튼이 올라갈 중앙을 비운다. 둥근 모서리 밖은 투명하다. [준비 패널](../../04_Resources/Images/ReadyPanelYellow.png)

## 로비 탭과 인벤토리

하단 탭은 1280×256 텍스처의 256px 칸 다섯 개다. 상점, 가방, 집, 말풍선, 톱니 순서를 유지하고, 글자는 UI에 맡긴다. Addressables 주소는 `Lobby/TabIcons`다. [로비 탭 원본](../../04_Resources/Images/LobbyTabIcons.png)

방어 카드용 초상은 배경을 투명하게 두고 이름·등급을 그리지 않는다. Unity가 카드별 정보를 겹쳐 표시하므로 인벤토리 크기에서도 얼굴과 장비가 함께 읽히는지 확인한다. [인벤토리에 표시한 초상](../../../Docs/Previews/LobbyDefense/LobbyDefenseInventory.png) · [카드 프레임과 상세 창 원화](../../../Docs/Previews/LobbyDefense/InventoryCardArt.md)

## 로비 디펜스 캐릭터

전신은 256×256 투명 PNG이며 몸과 장비를 합친 긴 쪽이 약 218px다. 카메라는 약간 위에서 내려다보고 얼굴과 장비가 겹치지 않게 한다. 기본 수비 유닛은 입구 쪽인 왼쪽, 침공 몬스터는 출구 쪽인 오른쪽을 본다. [수비 10종과 몬스터 5종 비교](../../../Docs/Previews/LobbyDefense/LobbyDefenseRoster.jpg) · [로비 디펜스 기획](../01_Architecture/LOBBY-DEFENSE.md)

## 프로필과 캐릭터 동작

프로필은 512×512 투명 단일 Sprite다. 얼굴과 어깨를 크게 잡고 장비의 특징이 보일 만큼 담는다. 눈·귀·장비 끝을 자르지 않고 하단에서 흉상을 자른다. 카드 프레임과 등급 표시는 UI에서 겹친다. [프로필 원본](../../04_Resources/Images/LobbyDefense/Profiles)

동작은 투명한 640×640 시트에 320×320 네 칸을 배치한다. 재생 순서는 왼쪽 위 → 오른쪽 위 → 왼쪽 아래 → 오른쪽 아래다. 중앙의 256px 전신 영역과 발 기준선을 맞추고 사방의 32px 여백으로 장비와 동작의 폭을 받는다. Unity는 Sprite Mode Multiple, 프레임별 중앙 피벗, PPU 1을 사용한다. [원본 시트와 클립](../../04_Resources/Images/LobbyDefense/Motions)

| 대상·동작 | 재생 | 네 프레임에서 보여 줄 변화 |
|---|---|---|
| 수비 Idle | 4 FPS, 1초 반복 | 기본 자세 → 숨 들이쉬기 → 눈 깜빡임과 날숨 → 기본 자세로 연결 |
| 수비 Attack | 8 FPS, 0.5초 1회 | 준비 → 힘 모으기 → 타격·발사 → 회복. 세 번째 그림이 0.25초에 보인다. |
| 수비 Special | 6 FPS, 약 0.67초 1회 | 장기자랑을 준비하고 소품이나 표정으로 특징을 보여 준 뒤 대기로 돌아간다. 대기 중 터치하거나 15초 대기하면 재생하고 실제 공격이 시작되면 양보한다. |
| 침공 Walk | 8 FPS, 0.5초 반복 | 발을 교대하거나 꼬리로 떠가는 제자리 보행. 화면 이동은 게임이 담당한다. |
| 침공 Attack | 8 FPS, 0.5초 1회 | 인접한 길막 유닛 쪽으로 준비 → 당기기 → 접촉 → 회복. [종별 동작과 8방향](INVADER-ART.md) |

## 공격 투사체

장비에서 떨어져 날아가는 물체 한 개를 128×128 투명 단일 Sprite로 그린다. 긴 쪽은 약 112px다. 기본 진행 방향은 왼쪽이며, 꼬리가 있다면 오른쪽에 둔다. 고리 구멍과 파동 사이는 투명하게 비운다. [진주 방울](../../04_Resources/Images/LobbyDefense/Projectiles/PearlSentry/Attack.png) · [투사체 13종 비교](../../../Docs/Previews/LobbyDefense/LobbyDefenseProjectiles.jpg)

기본 공격용 투사체 7종과 Special 시안 6종이 있다. 성벽거북·조개방패 소라게·돌갑옷 천산갑의 Attack 시트는 몸이나 장비로 타격하는 자세를 담는다. 현재 전투 규칙에서는 진주 수비병이 진주 탄을 발사하고 성벽거북은 길을 막는다. [로비 전투 규칙](../../05_Table/LobbyDefenseRules.json)

미리보기에서는 일부 장기자랑에 탄을 합성한다. 게임에서 장기자랑은 피해가 없는 대기 연출이다. [동작 미리보기](../../../Docs/Previews/LobbyDefense/LobbyDefenseMotion.html#SeedFinch/Special) · [로비 표시 흐름](../01_Architecture/LOBBY-DEFENSE-ARCHITECTURE.md)

## 납품 전 확인

1. 전신은 청록 타일과 밝은 UI 위에서 32px로, 투사체는 24px로 확인한다. 특징적인 문양·얼굴·장비가 보이고 투명 테두리에 흰 매트나 바닥 그림자가 없어야 한다.
2. 파생 이미지는 전신과 나란히 놓아 눈 모양, 색, 장비의 손잡이와 끝 모양을 대조한다. 네 프레임을 같은 자리에 겹쳐 발 기준선과 머리 크기를 확인하고, 반복 동작은 마지막에서 처음으로 이어 본다.
3. 전신·프로필·투사체 Sprite는 중앙 피벗·PPU 1이다. 주소는 `LobbyDefense/Defenders/{ID}`, `LobbyDefense/Monsters/{ID}`, `LobbyDefense/Profiles/{ID}`, `LobbyDefense/Projectiles/{ID}/{Attack|Special}` 형식이다.
4. 동작의 Addressables 대상은 `AnimationClip`이다. 수비는 `LobbyDefense/Motions/{ID}/{동작}`, 침공은 끝에 `/{방향}`을 붙인다. PNG 시트는 클립이 참조한다. [실제 등록 목록](../../AddressableAssetsData/AssetGroups/BuildIn_Resource.asset)
