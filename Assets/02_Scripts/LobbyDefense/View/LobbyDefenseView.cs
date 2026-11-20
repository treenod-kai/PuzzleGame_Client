using System;
using System.Collections.Generic;
using LobbyDefense.Model;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>전체 경로와 확대 지도를 함께 보여 주고 배치·고용·회수 입력을 로비 컨트롤러에 전달한다.</summary>
    public sealed class LobbyDefenseView : MonoBehaviour
    {
        private const float StatusHeight = 132f;
        private const float ControlsHeight = 236f;
        private const float MapGap = 4f;
        private static readonly Color Panel = new Color32(19, 44, 49, 255);
        private static readonly Color Trim = new Color32(44, 86, 89, 255);
        private static readonly Color Ink = new Color32(242, 248, 237, 255);
        private static readonly Color Gold = new Color32(253, 208, 128, 255);
        private readonly Dictionary<string, Sprite> _defenders = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, Sprite> _monsters = new Dictionary<string, Sprite>();
        private readonly List<int> _oldRoute = new List<int>();
        private readonly List<int> _oldPreview = new List<int>();
        private TMP_FontAsset _font;
        private TMP_Text _gold;
        private TMP_Text _wave;
        private TMP_Text _spawn;
        private TMP_Text _window;
        private TMP_Text _hint;
        private TMP_Text _choice;
        private TMP_Text _confirmText;
        private Button _confirm;
        private Button _recall;
        private DefenseBoardGraphic _overview;
        private DefenseBoardGraphic _detail;
        private DefenseUnitMarkers _dots;
        private DefenseUnitMarkers _units;
        private DefenseProjectileView _projectiles;
        internal DefenseMotionLibrary Motions { get; private set; }
        private int _width;
        private int _height;
        private int _entrance;
        private int _exit;
        private int _columns;
        private float _first = -1f;
        private Vector2Int _labelColumns;
        private int _shownGold = -1;
        private bool[] _route;
        private bool[] _preview;
        private int _selected = -1;
        private int _range;
        private bool _canPlace;

        public event Action<int> CellSelected;
        public event Action<int> ChoiceChanged;
        public event Action Confirmed;
        public event Action Recalled;

        /// <summary>방어 보드 <paramref name="board"/>와 로비 폰트 <paramref name="font"/>로 배치 지도와 미니맵을 구성한다.</summary>
        public void Initialize(DefenseBoard board, TMP_FontAsset font)
        {
            _width = board.Width;
            _height = board.Height;
            _columns = Mathf.Min(8, _width);
            _entrance = board.Entrance;
            _exit = board.Exit;
            _font = font;
            _route = new bool[board.CellCount];
            _preview = new bool[board.CellCount];
            RectTransform panel = Rect("DefensePanel", transform, 0f, 0f, 1f, 1f);
            Paint(panel, Panel);
            RectTransform status = Rect("DefenseStatus", panel, 0f, 1f, 1f, 1f);
            status.offsetMin = Vector2.down * StatusHeight;
            _wave = Label("Wave", status, 0.035f, 0.50f, 0.33f, 1f, "", 36f, TextAlignmentOptions.Left);
            _spawn = Label("Spawn", status, 0.33f, 0.50f, 0.56f, 1f, "", 34f, TextAlignmentOptions.Center);
            _gold = Label("Gold", status, 0.56f, 0.50f, 0.77f, 1f, "", 36f, TextAlignmentOptions.Right);
            _gold.color = Gold;
            _overview = Grid(Rect("Overview", status, 0.79f, 0f, 0.965f, 1f));
            _overview.Initialize(board, _columns, true, FocusOverview, MoveWindow);
            _window = Label("VisibleColumns", status, 0.035f, 0f, 0.77f, 0.49f, "", 34f, TextAlignmentOptions.Left);

            // 상태와 조작 영역의 높이를 먼저 확보하고 남은 공간에 정사각형 칸 10행을 맞춘다.
            RectTransform mapArea = Rect("PlacementArea", panel, 0.035f, 0f, 0.965f, 1f);
            mapArea.offsetMin = Vector2.up * (ControlsHeight + MapGap);
            mapArea.offsetMax = Vector2.down * (StatusHeight + MapGap);
            RectTransform map = Rect("PlacementMap", mapArea, 0f, 0f, 1f, 1f);
            AspectRatioFitter fit = map.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio = (float)_columns / _height;
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _detail = Grid(map);
            _detail.Initialize(board, _columns, false, SelectCell, MoveWindow);
            _dots = new DefenseUnitMarkers(_overview, this, _width, true);
            _units = new DefenseUnitMarkers(_detail, this, _width, false);
            Motions = new DefenseMotionLibrary(_detail.transform);
            _projectiles = new DefenseProjectileView(_detail, _width);
            RectTransform controls = Rect("PlacementControls", panel, 0.035f, 0f, 0.965f, 0f);
            controls.offsetMax = Vector2.up * ControlsHeight;
            _hint = Label("Hint", controls, 0f, 0.795f, 1f, 1f, "", 36f, TextAlignmentOptions.Center);
            _choice = Label("Choice", controls, 0f, 0.59f, 1f, 0.795f, "", 36f, TextAlignmentOptions.Center);
            Control("Previous", controls, 0f, 0.02f, 0.18f, 0.59f, "이전", Previous, out _);
            Control("Next", controls, 0.19f, 0.02f, 0.37f, 0.59f, "다음", Next, out _);
            _recall = Control("Recall", controls, 0.395f, 0.02f, 0.62f, 0.59f, "회수", Recall, out _);
            _confirm = Control("Confirm", controls, 0.63f, 0.02f, 1f, 0.59f, "배치 확정", Confirm, out _confirmText);
            _confirm.targetGraphic.color = new Color32(46, 118, 100, 255);
            FocusOverview(_entrance);
        }

        /// <summary>골드 <paramref name="gold"/>, 웨이브 문구 <paramref name="wave"/>와 다음 소환 문구 <paramref name="spawn"/>를 갱신한다.</summary>
        public void SetStatus(int gold, string wave, string spawn)
        {
            if (_shownGold != gold)
            {
                _shownGold = gold;
                _gold.text = $"{gold:N0} 골드";
            }
            _wave.text = wave;
            _spawn.text = spawn;
        }

        /// <summary>선택 카드 <paramref name="choice"/>, 실행 문구 <paramref name="action"/>, 확정·회수 가능 여부 <paramref name="canConfirm"/>·<paramref name="canRecall"/>와
        /// 배치 안내 <paramref name="hint"/>를 적용한다.</summary>
        public void SetControls(string choice, string action, bool canConfirm, bool canRecall, string hint)
        {
            _choice.text = choice;
            _confirmText.text = action;
            _confirm.interactable = canConfirm;
            _recall.interactable = canRecall;
            _hint.text = hint;
        }

        /// <summary>기존 경로 <paramref name="currentPath"/>와 예상 경로 <paramref name="previewPath"/>, 선택 칸 <paramref name="selectedCell"/>의
        /// 사거리 <paramref name="range"/> 및 배치 가능 여부 <paramref name="canPlace"/>를 표시한다.</summary>
        public void SetRoute(IReadOnlyList<int> currentPath, IReadOnlyList<int> previewPath, int selectedCell, int range, bool canPlace)
        {
            bool routeChanged = UpdatePath(currentPath, _oldRoute, _route);
            bool previewChanged = UpdatePath(previewPath, _oldPreview, _preview);
            if (!routeChanged && !previewChanged && _selected == selectedCell && _range == range && _canPlace == canPlace)
            {
                return;
            }
            bool selectionChanged = _selected != selectedCell;
            _selected = selectedCell;
            _range = range;
            _canPlace = canPlace;
            _overview.SetRoute(_route, _preview, _selected, _range, canPlace);
            _detail.SetRoute(_route, _preview, _selected, _range, canPlace);
            if (selectionChanged)
            {
                UpdateWindowLabel();
            }
        }

        /// <summary>현재 전투 <paramref name="session"/>과 마지막 틱 이후 보간 비율 <paramref name="tickFraction"/>로 두 지도의 개체를 갱신한다.</summary>
        public void RenderUnits(DefenseSession session, float tickFraction)
        {
            _dots.Render(session, tickFraction);
            _projectiles.UpdateTargets(session.Tick, tickFraction);
            _units.Render(session, tickFraction);
            _projectiles.Render(session.Tick, tickFraction);
        }

        /// <summary>계정 교체나 저장 복구 때 <paramref name="game"/>의 발사만 지도에 연결한다.</summary>
        internal void BindCombat(DefenseCampaign game) => _projectiles.Bind(game);

        /// <summary><paramref name="defender"/>가 확대 지도에서 실제 탄의 표적을 향하도록 좌우 방향을 구한다.</summary>
        internal bool FacesRight(DeployedDefender defender) => _projectiles.FacesRight(defender);

        /// <summary>확대 지도에서 누른 <paramref name="defender"/>의 <paramref name="tick"/> 시점 대기 장기자랑을 요청한다.</summary>
        internal void RequestShowcase(DeployedDefender defender, long tick) => _units.RequestShowcase(defender, tick);

        /// <summary>로비를 떠날 때 현재 전투의 발사 구독을 해제한다.</summary>
        private void OnDestroy()
        {
            if (_projectiles != null)
            {
                _projectiles.Unsubscribe();
            }
        }

        /// <summary>새 경로 <paramref name="path"/>가 이전 경로 <paramref name="old"/>와 달라질 때만 칸 표시 배열 <paramref name="mask"/>을 다시 채운다.</summary>
        private static bool UpdatePath(IReadOnlyList<int> path, List<int> old, bool[] mask)
        {
            int count = path == null ? 0 : path.Count;
            bool changed = old.Count != count;
            for (int i = 0; !changed && i < count; i++)
            {
                changed = old[i] != path[i];
            }
            if (!changed)
            {
                return false;
            }
            for (int i = 0; i < old.Count; i++)
            {
                mask[old[i]] = false;
            }
            old.Clear();
            for (int i = 0; i < count; i++)
            {
                mask[path[i]] = true;
                old.Add(path[i]);
            }
            return true;
        }

        /// <summary>진영 <paramref name="group"/>과 종류 <paramref name="id"/>의 스프라이트를 씬의 공용 에셋 관리자에서 한 번 요청한다.</summary>
        internal Sprite FindSprite(string group, string id)
        {
            Dictionary<string, Sprite> cache = group == "Monsters" ? _monsters : _defenders;
            if (group == "Trap")
            {
                id = "CoilFrog/Special";
            }
            if (cache.TryGetValue(id, out Sprite sprite))
            {
                return sprite;
            }
            cache.Add(id, null);
            string address = group == "Trap" ? "LobbyDefense/Projectiles/CoilFrog/Special" : $"LobbyDefense/{group}/{id}";
            LoadSprite(cache, id, address);
            return cache[id];
        }

        /// <summary>주소 <paramref name="address"/>의 비동기 로드 결과를 <paramref name="cache"/>의 종류 키 <paramref name="id"/>에 기록한다.</summary>
        private static void LoadSprite(Dictionary<string, Sprite> cache, string id, string address)
        {
            AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<Sprite>
            {
                address = address,
                successCallback = loaded => cache[id] = loaded,
                failedCallback = () => Debug.LogError($"로비 디펜스 이미지를 불러오지 못했습니다. {address}")
            });
        }

        /// <summary>전체 지도에서 누른 칸 <paramref name="cell"/>의 열을 배치 지도 가운데로 옮긴다.</summary>
        private void FocusOverview(int cell) => MoveWindow(cell % _width + 0.5f - _columns * 0.5f);

        /// <summary>왼쪽 열 좌표 <paramref name="first"/>를 보드 안으로 제한하고 두 지도가 같은 구역을 가리키게 한다.</summary>
        private void MoveWindow(float first)
        {
            first = Mathf.Clamp(first, 0f, _width - _columns);
            if (Mathf.Approximately(first, _first))
            {
                return;
            }
            _first = first;
            Rect window = new Rect(first, 0f, _columns, _height);
            _overview.SetWindow(window);
            _detail.SetWindow(window);
            Vector2Int columns = new Vector2Int(Mathf.FloorToInt(first) + 1, Mathf.CeilToInt(window.xMax));
            if (_labelColumns != columns)
            {
                _labelColumns = columns;
                UpdateWindowLabel();
            }
        }

        /// <summary>확대 창의 열 범위와 입출구 방향, 선택한 칸의 기획 좌표를 보여 준다.</summary>
        private void UpdateWindowLabel()
        {
            string inlet = GateLabel("입구", _entrance);
            string outlet = GateLabel("출구", _exit);
            string selection = _selected < 0 ? "" : $"   선택 ({_selected % _width + 1}, {_selected / _width + 1})";
            _window.text = $"{inlet}   {_labelColumns.x}~{_labelColumns.y}열   {outlet}{selection}";
        }

        /// <summary>입구 또는 출구 <paramref name="name"/>의 칸 <paramref name="cell"/>이 확대 창 밖이면 방향을 표시한다.</summary>
        private string GateLabel(string name, int cell)
        {
            int column = cell % _width + 1;
            if (column < _labelColumns.x)
            {
                return name + " (왼쪽)";
            }
            if (column > _labelColumns.y)
            {
                return name + " (오른쪽)";
            }
            return name;
        }

        /// <summary>선택한 칸 <paramref name="cell"/>을 컨트롤러에 전달한다.</summary>
        private void SelectCell(int cell)
        {
            if (CellSelected != null)
            {
                CellSelected(cell);
            }
        }

        /// <summary>이전 카드나 고용 후보를 요청한다.</summary>
        private void Previous()
        {
            if (ChoiceChanged != null)
            {
                ChoiceChanged(-1);
            }
        }

        /// <summary>다음 카드나 고용 후보를 요청한다.</summary>
        private void Next()
        {
            if (ChoiceChanged != null)
            {
                ChoiceChanged(1);
            }
        }

        /// <summary>현재 후보로 배치 확정을 요청한다.</summary>
        private void Confirm()
        {
            if (Confirmed != null)
            {
                Confirmed();
            }
        }

        /// <summary>선택한 칸의 유닛 회수를 요청한다.</summary>
        private void Recall()
        {
            if (Recalled != null)
            {
                Recalled();
            }
        }

        /// <summary>지도 영역 <paramref name="rect"/> 바깥의 표시를 잘라낸다.</summary>
        private static DefenseBoardGraphic Grid(RectTransform rect)
        {
            rect.gameObject.AddComponent<RectMask2D>();
            return rect.gameObject.AddComponent<DefenseBoardGraphic>();
        }

        /// <summary><paramref name="parent"/>의 상대 영역 <paramref name="x0"/>·<paramref name="y0"/>~<paramref name="x1"/>·<paramref name="y1"/>에 이름 <paramref name="name"/>의 UI를 둔다.</summary>
        internal static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            RectTransform rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>영역 <paramref name="rect"/>에 입력을 받지 않는 <paramref name="tint"/> 배경을 추가한다.</summary>
        internal static Image Paint(RectTransform rect, Color tint)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = tint;
            image.raycastTarget = false;
            return image;
        }

        /// <summary><paramref name="parent"/> 안의 <paramref name="x0"/>·<paramref name="y0"/>~<paramref name="x1"/>·<paramref name="y1"/>에 <paramref name="name"/> 라벨을 둔다.
        /// <paramref name="value"/>를 최대 크기 <paramref name="size"/>와 정렬 <paramref name="alignment"/>로 표시한다.</summary>
        private TMP_Text Label(string name, Transform parent, float x0, float y0, float x1, float y1, string value, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = Rect(name, parent, x0, y0, x1, y1);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                text.font = _font;
            }
            text.text = value;
            text.color = Ink;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        /// <summary><paramref name="parent"/> 안의 <paramref name="x0"/>·<paramref name="y0"/>~<paramref name="x1"/>·<paramref name="y1"/>에 이름 <paramref name="name"/>의 버튼을 둔다.
        /// 문구 <paramref name="value"/>를 담은 <paramref name="label"/>과 눌렀을 때의 <paramref name="click"/>을 연결한다.</summary>
        private Button Control(string name, Transform parent, float x0, float y0, float x1, float y1, string value, UnityAction click, out TMP_Text label)
        {
            RectTransform rect = Rect(name, parent, x0, y0, x1, y1);
            Image image = Paint(rect, Trim);
            image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            label = Label("Label", rect, 0.03f, 0.03f, 0.97f, 0.97f, value, 34f, TextAlignmentOptions.Center);
            return button;
        }
    }
}
