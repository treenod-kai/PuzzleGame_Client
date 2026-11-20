# 방어 카드 인벤토리 원화

진주 수비병의 조개 장식과 청록색 망토에서 카드 테두리와 등급 띠의 색을 가져왔다. 초상, 이름, 등급, 능력치는 Unity에서 보유 카드마다 표시한다. [목록 화면](LobbyDefenseInventory.png) · [상세 창](LobbyDefenseCardDetail.png)

| 생성한 리소스 | 게임 적용 |
| --- | --- |
| [카드 프레임](../../../Assets/04_Resources/Images/LobbyDefense/InventoryCardFrame.png) | 위쪽에 초상을 배치하고 아래쪽 청록색 띠에 별과 유닛 이름을 표시한다. |
| [상세 창 배경](../../../Assets/04_Resources/Images/LobbyDefense/InventoryDetailPanel.png) | 물결 위쪽에는 초상과 등급을, 크림색 본문에는 종류별 에너지와 개별 능력치를 배치한다. |
| [에나멜 버튼](../../../Assets/04_Resources/Images/LobbyDefense/InventoryEnamelButton.png) | 9분할 스프라이트를 강화·변환 버튼과 상태 표시에 사용한다. 선택·눌림·비활성 색은 버튼이 결정한다. |

## 내장 ImageGen 생성 프롬프트

세 이미지 모두 투명 배경으로 생성했다.

### 카드 프레임

> Single production-ready 4:5 portrait collectible character CARD BLANK for a polished Korean chibi puzzle-defense game. Straight-on orthographic, isolated on actual transparent alpha, tightly framed, rounded corners, symmetrical silhouette. It is a premium little enamel tidepool trading card, not medieval: thin deep petrol outer edge, small warm honey-gold inner bevel, one shell-notch detail at each top corner. Top 68% is a luminous pale seafoam-to-aqua portrait stage, subtle curved light ripples near its side edges, empty and calm in the middle for a character composited later. Bottom 30% is one uninterrupted rich deep teal lacquer nameplate, gently curved at the seam, empty with enough flat room for two lines of live labels. Warm dark ink contours, soft upper-left specular highlight, broad clean hand-painted shapes like glossy toy puzzle pieces. The upper field and lower nameplate are opaque. No character, lettering, digits, stars, gems, badge slots, extra boxes, outlying shadows, prop mockup, or backdrop. Fill the canvas almost edge to edge.

### 상세 창 배경

> Single production-ready portrait ~4:5 card DETAIL WINDOW skin for a polished Korean chibi puzzle-defense game, centered isolated on genuine transparent alpha. Flat straight-on orthographic UI asset, fills the canvas, softly rounded rectangle with thin deep petrol contour, modest honey-gold inner lip, soft upper-left enamel highlight. A quiet seafoam-to-turquoise illustrated header occupies exactly the upper 30%: subtle underwater light shafts, delicate wave gleams at side corners, space for a portrait on the left and dark live title on the right. A shallow curving wave seam joins it to one broad uninterrupted warm ivory writing surface that fills the bottom 70%. Both fields opaque. Hand-painted enamel toy finish and restrained marine details, designed for legibility at phone size. Clean silhouette. No lettering, numerals, characters, stars, badges, icons, grids, buttons, pedestals, extra frames or cast shadow. Only the window with its blank fields.

### 버튼

> A single isolated blank horizontal capsule button sprite for a chibi puzzle mobile game, aspect ratio 3:1. Straight-on orthographic, tightly cropped. Simple premium molded porcelain enamel with a broad smooth PEARL WHITE face, slender cool light grey edge and a slightly darker bevel on the lower edge. Pillowed rounded corners, subtle continuous highlight across the upper lip, blank quiet central face for a Korean action label. This master artwork will be tinted jade and honey in Unity. Exact bilateral symmetry, opaque button interior, outside transparent alpha. No letters, glyphs, decoration, fastening details, icons, sparkles or external cast shadow.
