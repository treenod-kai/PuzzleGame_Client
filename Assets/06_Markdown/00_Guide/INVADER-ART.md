# 침공 몬스터 방향과 봉쇄 공격

[5종의 걷기·공격을 한눈에 보기](InvaderDirections.gif) · [방향을 바꿔 공격 시험하기](LobbyDefenseMotion.html#PebbleCrab/Attack/NorthEast) · [게임 이미지 스타일](ART-STYLE.md#프로필과-캐릭터-동작)

몸 방향은 위에서 내려다본 보드 기준이다. 위는 등, 아래는 얼굴을 정면으로 보여 준다. 오른쪽 위는 등과 옆구리, 오른쪽 아래는 얼굴과 가슴이 함께 보인다. 왼쪽 세 방향에서는 대비되는 오른쪽 원화를 가로로 반전한다. 조약돌 집게의 큰 집게도 화면상 좌우가 함께 바뀐다. 보드의 경로 기획은 상하좌우 네 이웃을 사용하며, 표시할 방향은 이동이나 공격 대상에 맞춰 선택한다.

걷기는 8 FPS로 4프레임을 반복한다. 봉쇄 공격은 같은 속도로 준비 → 힘 모으기 → 접촉 → 회복을 한 번 재생한다. 접촉 그림은 재생 시작 후 0.25초에 나타난다. 0.5초는 클립 길이이며, 다음 공격 시각은 전투 모델에서 결정한다. 각 적은 자기 칸에서 출구로 가는 길이 열려 있으면 이동하고, 끊겼을 때 막는 유닛의 인접 칸에서 공격한다. [봉쇄 시 행동 규칙](../01_Architecture/LOBBY-DEFENSE.md#세로-한-열을-막았을-때의-심판)

| 몬스터 | 오른쪽 공격 시트 | 공격 실루엣 |
|---|---|---|
| 물방울 젤리 | [공격 시트](../../04_Resources/Images/LobbyDefense/Motions/WaveJelly/WaveJelly_Attack.png) | 몸을 납작하게 눌렀다가 이마로 튕겨 부딪친다. |
| 조약돌 집게 | [공격 시트](../../04_Resources/Images/LobbyDefense/Motions/PebbleCrab/PebbleCrab_Attack.png) | 큰 집게를 벌려 뒤로 당긴 뒤 앞으로 뻗어 닫는다. |
| 갈대도마뱀 | [공격 시트](../../04_Resources/Images/LobbyDefense/Motions/ReedRunner/ReedRunner_Attack.png) | 목을 움츠렸다가 턱을 내밀어 짧게 문다. |
| 별핵도깨비 | [공격 시트](../../04_Resources/Images/LobbyDefense/Motions/ScoreImp/ScoreImp_Attack.png) | 두 발을 딛고 몸을 비틀어 수정 주먹을 내민다. |
| 길지기 유령 | [공격 시트](../../04_Resources/Images/LobbyDefense/Motions/PathWarden/PathWarden_Attack.png) | 공중에 떠서 두 손을 모았다가 앞손으로 밀어낸다. |

## Unity에서 재생

방향별 클립의 Addressables 키는 `LobbyDefense/Motions/{몬스터 ID}/{Walk 또는 Attack}/{방향}`이다. 예를 들어 `LobbyDefense/Motions/ScoreImp/Attack/NorthWest`를 `AnimationClip`으로 로드하면 왼쪽 위를 때린다. 같은 오브젝트의 `SpriteRenderer.m_Sprite`와 `m_FlipX`를 클립에서 함께 설정하므로 매번 방향이 갱신된다. 전투 뷰는 [AssetManager](../../02_Scripts/Manager/AssetManager.cs)를 통해 에셋을 로드한다. 방향을 생략한 기본 주소는 오른쪽 Walk·Attack이다.

| 방향 키 | 화면 | 사용하는 원화 | 가로 반전 |
|---|---|---|---|
| `North` | 위 | 위 | 꺼짐 |
| `NorthEast` | 오른쪽 위 | 오른쪽 위 | 꺼짐 |
| `East` | 오른쪽 | 오른쪽 | 꺼짐 |
| `SouthEast` | 오른쪽 아래 | 오른쪽 아래 | 꺼짐 |
| `South` | 아래 | 아래 | 꺼짐 |
| `SouthWest` | 왼쪽 아래 | 오른쪽 아래 | 켜짐 |
| `West` | 왼쪽 | 오른쪽 | 켜짐 |
| `NorthWest` | 왼쪽 위 | 오른쪽 위 | 켜짐 |

각 시트는 640 × 640 RGBA PNG다. 왼쪽 위부터 두 줄로 320 × 320 프레임 네 장이 들어가며 중앙 피벗과 PPU 1을 사용한다. 다섯 몬스터의 두 동작은 원화 시트 50장과 방향별 클립 80개로 구성된다. [몬스터별 시트와 클립](../../04_Resources/Images/LobbyDefense/Motions)

## 생성 프롬프트

내장 ImageGen에 기존 몬스터 PNG를 첨부했다. 새 방향의 걷기에는 공통 지시문과 해당 방향·종의 걷기 지시문을 썼다. 공격에는 같은 방향의 걷기 준비 자세도 참조로 첨부했다. 최초 오른쪽 공격은 기존 몬스터의 오른쪽 원화를 기준으로 만들었다.

<details><summary>방향 및 종별 지시문</summary>

Create a production-ready animation sprite sheet for the SAME SINGLE creature as the input. Input gives the character identity, materials, natural proportions and paint style. RENDER IT FROM THE SPECIFIED NEW VIEW: actually rotate the 3D creature about its vertical axis before painting; NEVER turn a flat side-view drawing in the image plane. Orthographic slightly overhead game-board camera with ground plane consistently visible from above, head always upright, lighting from screen upper LEFT. The floor directions below are in screen space. Keep camera and facing locked in all four poses. Exactly 4 whole-body sprites on true transparent alpha, equal square 2x2 grid in reading order: top-left, top-right, bottom-left, bottom-right. Empty transparent gutters, no panel borders. Fit the whole character within central 72% of EACH cell, same scale and stable root. No floor, drop shadows, overlays, text, props, targets, arrows or backgrounds. Bold, clean 2D enamel/jelly shapes legible at 32 px.

| 방향 | 지시문 |
|---|---|
| 위 | NORTH / straight UP the board at 12 o'clock. DIRECT REAR view, camera behind its spine: viewer sees BACK of the head, back, and rear surfaces; face, mouth, eyes and chest completely occluded. Snout or leading hands at the TOP, tail toward BOTTOM foreground. Centered axis, symmetric back view. |
| 아래 | SOUTH / straight DOWN at 6 o'clock TOWARD the viewer. Direct FRONT view, centered nearly symmetrical face, BOTH eyes and front of chest or belly visible. Both cheeks equal in size. Near feet or hands lead toward bottom center. No nose pointing to either SIDE. Foreshorten body length and let tail peek behind. |
| 오른쪽 위 | NORTH-EAST diagonally UP-RIGHT at 1:30, AWAY from the viewer. Camera sees THREE-QUARTER REAR view: back of the head, dominant expanse of its back, near flank; face mostly hidden with at most a narrow far-right sliver of ONE eye. Nose/leading limb top RIGHT, hips and tail trail toward bottom LEFT. Clearly an oblique receding rear view. |
| 오른쪽 아래 | SOUTH-EAST diagonally DOWN-RIGHT at 4:30, TOWARD the viewer. THREE-QUARTER FRONT view with prominent FRONT of muzzle/chest, BOTH eyes visible, snout leading at lower RIGHT and tail receding behind at upper LEFT. The nearer rightward-reaching limb is slightly larger through foreshortening. Distinct from side profile and rear view. |

걷기 공통 문구는 아래와 같다.

A seamless FOUR-phase movement loop in place.

공격 공통 문구는 아래와 같다.

Animate one clear adjacent MELEE attack along this exact bearing. Four distinct keyframes in this order: 1 natural ready stance, 2 visibly gather backwards and compress, 3 commit the species-specific blow toward the indicated target (use foreshortening for front and rear views), 4 recoil to the ready stance. Reference A provides identity, reference B when present shows the locked directional walk pose to match. No opponent or projectile in the sheet.

| 몬스터 | 걷기 | 공격 |
|---|---|---|
| 물방울 젤리 | A glossy coral-pink tadpole with a large wave tail and two tiny pink feet. Four poses: planted forward foot with tail raised, low squash with feet passing, spring up with opposite foot leading, rear-foot push-off with the tail following. Keep the directional view locked. | Squash the coral tadpole and forehead-bump forward along the specified bearing, then wobble back. Two tiny feet remain near the root; make the wave tail counterbalance. |
| 조약돌 집게 | A bright orange crab sheltered by its single amber pebble shell, chunky large pincer and little rear legs. In-place scuttle in four phases: one set of feet extends, legs pass and shell dips, opposite feet extend, shell rises on push-off. The large pincer leads in the travel direction without an attack snap. | Open and draw back the one large pincer, extend and snap it shut against the imaginary adjacent blocker along the specified bearing; partly reopen in recovery. Orange legs brace beneath the amber pebble shell. |
| 갈대도마뱀 | A slim green newt with a long tail and two yellow dorsal fins. Four-phase quadruped trot showing alternating diagonal feet, limbs passing under the belly, with the long trailing tail bending the other way. Facing stays locked to travel. | Bunch the green newt’s neck, drive its head into a short emphatic snapping bite along the specified bearing, then recoil with jaw closed, feet planted, two yellow dorsal fins and long tail counterbalancing. |
| 별핵도깨비 | A faceted purple imp with two pointed ears and tiny limbs. It has a gold star INSET IN THE FRONT of its belly, visible only from front and front-three-quarter! Its BACK is plain plum crystal with NO star. Four-phase alternating foot march with opposite arm swings, low passing then tall passing. | Plant tiny feet, rotate the shoulders backwards, deliver a single short crystalline fist punch along the specified bearing, then recover. Gold star is set only in the FRONT belly. Rear view has a plain faceted purple back; no rear star. |
| 길지기 유령 | Pearl-white and seafoam floating ghost with mitten hands and a single curling tail instead of legs. A four-phase glide: lean forward, lift one mitten and curl the tail, lead with the other mitten, tail sweeps back for a seamless loop. No feet. The back has no face. | Draw both mittens back on the hovering mint ghost, then forcefully shove the leading open mitten followed closely by the other along the specified bearing; hands retract and floating tail relaxes on recovery. Never add legs or feet. |

오른쪽 공격의 공통 문구와 종별 지시문은 아래와 같다.

Use case: identity-preserve. This single attached monster PNG is the exact identity reference for an in-game animated melee attack sprite sheet. Paint FOUR consecutive DIFFERENT full body poses of that very same creature, viewed from the same orthographic three-quarter side view facing screen RIGHT, attacking an unseen blocker adjacent on its right. Hand-painted glossy enamel / jelly, same flat shading, contour, upper-left highlight, markings, face, coloring and limb count as the reference. Square 2 by 2 sheet. Read TOP LEFT ready (match the reference), TOP RIGHT recoils and visibly winds up, BOTTOM LEFT forcefully extends to the RIGHT making contact, BOTTOM RIGHT releases and recovers toward the original pose. One complete monster inside the central 72% of EACH of the four equal square quadrants. Same head size and fixed camera, grounded or floating baseline consistent; generous empty fully transparent gutters and edges. Keep entire tail, tips and limbs inside every cell. Genuine clean transparent alpha, no surface, backplate, shadow, soft atmospheric halo, other characters, blocker, projectiles, markings outside the character, text, labels or panel borders. Strong physical silhouettes and a more extended reach in pose 3; compact and clear at small game size.

| 몬스터 | 오른쪽 공격 |
|---|---|
| 물방울 젤리 | A coral-pink jelly tadpole with two tiny feet and a raised wave tail. Crouch down and pull the face back on frame two; spring into a stretched horizontal forehead bump to the right on frame three, pushing from the back foot; wobble back round with the tail tipping the opposite way in frame four. Keep the dark pink eyes and broad pearl highlight. |
| 조약돌 집게 | A tiny orange shore crab under its single amber pebble shell. Open and draw the large RIGHT-facing front pincer back and up in frame two; extend that big pincer straight forward, visibly snap it shut in frame three; draw it back and partly reopen in frame four. Rear feet stay planted and shell does not turn. |
| 갈대도마뱀 | A bright spring-green newt with two butter-yellow dorsal fins and a long green tail trailing left. Tuck chin and coil the neck in frame two. Drive its head forward to the right with a low, emphatic snapping bite in frame three, hind feet planted and tail counterbalancing behind. Head rebounds and jaw closes in frame four; no breath attack or projectile. |
| 별핵도깨비 | A faceted plum-colored imp with two upright ears, stubby limbs and one very prominent GOLD star inset in its belly. Plant its feet and twist back with the near fist drawn to the left in frame two; sharply punch the near fist to screen right in frame three with a forward weight shift; draw it back and reopen its stance in frame four. The star stays attached to the belly in all poses. |
| 길지기 유령 | A hovering pearl-white mint ghost, swept floating tail and two rounded mitten hands. Bunch the tail and pull both mittens back in frame two; sharply thrust its leading mitten outward with the second just behind, giving a stern rightward spectral palm shove on frame three. Pull hands back and uncurl the tail in frame four. Keep the floating tail, absolutely no feet, no detached pulse or spell. |

</details>
