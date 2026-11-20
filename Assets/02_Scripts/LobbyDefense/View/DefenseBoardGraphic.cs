using System;
using LobbyDefense.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>전체 지도와 확대 지도에서 칸·경로·사거리를 한 메시로 그리고 포인터를 보드 좌표로 바꾼다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DefenseBoardGraphic : MaskableGraphic, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Color Empty = new Color32(29, 67, 71, 255);
        private static readonly Color Alternate = new Color32(33, 73, 76, 255);
        private static readonly Color Route = new Color32(81, 136, 127, 255);
        private static readonly Color Preview = new Color32(170, 210, 141, 255);
        private static readonly Color Gate = new Color32(242, 183, 90, 255);
        private static readonly Color Invalid = new Color32(228, 116, 94, 255);
        private static readonly Color SpawnArea = new Color32(71, 79, 55, 255);
        private static readonly Color SpawnPath = new Color32(150, 153, 93, 255);
        private DefenseBoard _board;
        private int _width;
        private int _height;
        private int _entrance;
        private int _exit;
        private Rect _window;
        private RectTransform _frame;
        private bool _overview;
        private int _fitColumns;
        private int _candidate = -1;
        private int _range;
        private bool _canPlace;
        private bool[] _route;
        private bool[] _preview;
        private Action<int> _select;
        private Action<float> _pan;
        private int? _dragPointerId;
        private bool _dragging;
        private Vector2 _lastDragPoint;

        internal float TileSize => Mathf.Min(rectTransform.rect.width / _fitColumns, rectTransform.rect.height / _height);
        private Rect VisibleCells => _overview ? new Rect(0f, 0f, _width, _height) : _window;

        /// <summary>보드 <paramref name="board"/>에 표시할 열 수 <paramref name="columns"/>와 전체 보기 <paramref name="overview"/>에 맞춰
        /// 칸 선택 <paramref name="select"/> 및 가로 이동 <paramref name="pan"/>을 연결한다.</summary>
        internal void Initialize(DefenseBoard board, int columns, bool overview, Action<int> select, Action<float> pan)
        {
            _board = board;
            _width = board.Width;
            _height = board.Height;
            _entrance = board.Entrance;
            _exit = board.Exit;
            _overview = overview;
            _fitColumns = overview ? _width : columns;
            _select = select;
            _pan = pan;
            if (overview)
            {
                _frame = LobbyDefenseView.Rect("VisibleArea", transform, 0.5f, 0.5f, 0.5f, 0.5f);
                AddFrameEdge("Top", 0f, 1f, 1f, 1f, new Vector2(0f, -2f), Vector2.zero);
                AddFrameEdge("Bottom", 0f, 0f, 1f, 0f, Vector2.zero, new Vector2(0f, 2f));
                AddFrameEdge("Left", 0f, 0f, 0f, 1f, Vector2.zero, new Vector2(2f, 0f));
                AddFrameEdge("Right", 1f, 0f, 1f, 1f, new Vector2(-2f, 0f), Vector2.zero);
            }
        }

        /// <summary>테두리 <paramref name="name"/>를 앵커 <paramref name="x0"/>·<paramref name="y0"/>~<paramref name="x1"/>·<paramref name="y1"/>에 붙이고
        /// 안쪽 여백 <paramref name="min"/>·<paramref name="max"/>로 선 두께를 설정한다.</summary>
        private void AddFrameEdge(string name, float x0, float y0, float x1, float y1, Vector2 min, Vector2 max)
        {
            RectTransform edge = LobbyDefenseView.Rect(name, _frame, x0, y0, x1, y1);
            edge.offsetMin = min;
            edge.offsetMax = max;
            LobbyDefenseView.Paint(edge, Gate);
        }

        /// <summary>칸 단위의 확대 영역 <paramref name="window"/>를 상세 지도와 미니맵 테두리에 반영한다.</summary>
        internal void SetWindow(Rect window)
        {
            _window = window;
            if (_overview)
            {
                PositionFrame();
            }
            else
            {
                SetVerticesDirty();
            }
        }

        /// <summary>창을 움직일 때는 미니맵의 지형 메시를 재생성하지 않고 테두리만 옮긴다.</summary>
        private void PositionFrame()
        {
            _frame.anchoredPosition = CellCenter(_window.center.x - 0.5f, _window.center.y - 0.5f);
            _frame.sizeDelta = _window.size * TileSize;
        }

        /// <summary>미니맵 자체의 크기가 바뀌면 새 축척에 맞춰 테두리도 맞춘다.</summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_frame != null)
            {
                PositionFrame();
            }
        }

        /// <summary>실제 길 <paramref name="route"/>·예상 길 <paramref name="preview"/>, 후보 <paramref name="candidate"/>의
        /// 사거리 <paramref name="range"/>와 배치 가능 여부 <paramref name="canPlace"/>를 표시한다.</summary>
        internal void SetRoute(bool[] route, bool[] preview, int candidate, int range, bool canPlace)
        {
            _route = route;
            _preview = preview;
            _candidate = candidate;
            _range = range;
            _canPlace = canPlace;
            SetVerticesDirty();
        }

        /// <summary>보드 좌표 <paramref name="column"/>·<paramref name="row"/>의 중심을 로컬 좌표로 환산한다.</summary>
        internal Vector2 CellCenter(float column, float row)
        {
            float tile = TileSize;
            Vector2 center = VisibleCells.center;
            return rectTransform.rect.center + new Vector2(column + 0.5f - center.x, center.y - row - 0.5f) * tile;
        }

        /// <summary>보드 좌표 <paramref name="column"/>·<paramref name="row"/>의 유닛이 화면에 일부라도 보이는지 확인한다.</summary>
        internal bool Shows(float column, float row) => _overview || (column + 1f > _window.xMin && column < _window.xMax &&
            row + 1f > _window.yMin && row < _window.yMax);

        /// <summary>메시 작성기 <paramref name="vh"/>에 보이는 칸의 경로·등장 구역과 배치 후보를 그린다.</summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_width == 0 || TileSize <= 0f)
            {
                return;
            }
            float tile = TileSize;
            float half = tile * (_overview ? 0.5f : 0.47f);
            Rect visible = VisibleCells;
            Rect bounds = rectTransform.rect;
            Vector2 origin = CellCenter(0f, 0f);
            int firstColumn = Mathf.Max(0, Mathf.FloorToInt(visible.xMin));
            int endColumn = Mathf.Min(_width, Mathf.CeilToInt(visible.xMax));
            for (int row = 0; row < _height; row++)
            {
                for (int column = firstColumn; column < endColumn; column++)
                {
                    int cell = row * _width + column;
                    Vector2 p = origin + new Vector2(column, -row) * tile;
                    Color tint = (column + row) % 2 == 0 ? Empty : Alternate;
                    if (_candidate >= 0 && !_overview &&
                        Mathf.Abs(column - _candidate % _width) + Mathf.Abs(row - _candidate / _width) <= _range)
                    {
                        tint = Color.Lerp(tint, Preview, 0.28f);
                    }
                    if (_route != null && _route[cell])
                    {
                        tint = Route;
                    }
                    if (_preview != null && _preview[cell])
                    {
                        tint = Preview;
                    }
                    if (_board.IsSpawnArea(cell))
                    {
                        bool onPath = (_route != null && _route[cell]) || (_preview != null && _preview[cell]);
                        tint = onPath ? SpawnPath : SpawnArea;
                    }
                    if (cell == _entrance || cell == _exit)
                    {
                        tint = Gate;
                    }
                    if (cell == _candidate)
                    {
                        tint = _canPlace ? Color.white : Invalid;
                    }
                    float x0 = Mathf.Max(p.x - half, bounds.xMin);
                    float x1 = Mathf.Min(p.x + half, bounds.xMax);
                    float y0 = Mathf.Max(p.y - half, bounds.yMin);
                    float y1 = Mathf.Min(p.y + half, bounds.yMax);
                    if (x1 > x0 && y1 > y0)
                    {
                        Quad(vh, x0, y0, x1, y1, tint);
                    }
                }
            }
        }

        /// <summary><paramref name="vh"/>에 좌하단 <paramref name="x0"/>·<paramref name="y0"/>과 우상단 <paramref name="x1"/>·<paramref name="y1"/> 사이의 <paramref name="tint"/> 사각형을 더한다.</summary>
        private static void Quad(VertexHelper vh, float x0, float y0, float x1, float y1, Color32 tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), tint, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), tint, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), tint, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>클릭한 <paramref name="eventData"/>의 좌표가 실제 칸에 들어오면 선택을 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!eventData.eligibleForClick || eventData.dragging || eventData.button != PointerEventData.InputButton.Left ||
                !LocalPoint(eventData, out Vector2 local))
            {
                return;
            }
            Vector2 point = BoardPoint(local);
            int column = Mathf.FloorToInt(point.x);
            int row = Mathf.FloorToInt(point.y);
            if (VisibleCells.Contains(point) && column >= 0 && column < _width && row >= 0 && row < _height)
            {
                _select(row * _width + column);
            }
        }

        /// <summary>지도 안의 로컬 좌표 <paramref name="local"/>를 보드 왼쪽 위 기준의 연속 칸 좌표로 바꾼다.</summary>
        private Vector2 BoardPoint(Vector2 local)
        {
            Vector2 offset = (local - rectTransform.rect.center) / TileSize;
            return VisibleCells.center + new Vector2(offset.x, -offset.y);
        }

        /// <summary>종료 이벤트가 누락된 제스처도 새로 누른 <paramref name="eventData"/>의 포인터로 교체한다.</summary>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _dragPointerId = eventData.pointerId;
                _dragging = false;
            }
        }

        /// <summary>입력 그룹이 바뀌면 진행 중인 포인터 제스처를 버린다.</summary>
        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();
            _dragPointerId = null;
            _dragging = false;
        }

        /// <summary>지도가 닫히면 누르고 있던 포인터 상태도 해제한다.</summary>
        protected override void OnDisable()
        {
            _dragPointerId = null;
            _dragging = false;
            base.OnDisable();
        }

        /// <summary>드래그를 시작한 <paramref name="eventData"/>의 누른 위치를 기록하고 칸 선택을 취소한다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            eventData.eligibleForClick = false;
            if (eventData.button == PointerEventData.InputButton.Left && _dragPointerId == eventData.pointerId &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.pressPosition, eventData.pressEventCamera, out _lastDragPoint))
            {
                _dragging = true;
            }
        }

        /// <summary><paramref name="eventData"/>의 이동을 따라 확대 지도를 밀거나 미니맵의 해당 위치로 창을 옮긴다.</summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || eventData.button != PointerEventData.InputButton.Left || _dragPointerId != eventData.pointerId ||
                !LocalPoint(eventData, out Vector2 local))
            {
                return;
            }
            if (_overview)
            {
                _pan(BoardPoint(local).x - _window.width * 0.5f);
            }
            else
            {
                _pan(_window.xMin + (_lastDragPoint.x - local.x) / TileSize);
            }
            _lastDragPoint = local;
        }

        /// <summary><paramref name="eventData"/>의 드래그가 끝나면 다음 터치가 지도를 조작할 수 있게 한다.</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _dragPointerId == eventData.pointerId)
            {
                _dragPointerId = null;
                _dragging = false;
            }
        }

        /// <summary>입력 <paramref name="data"/>의 위치를 지도 안의 로컬 좌표 <paramref name="local"/>로 바꾼다.</summary>
        private bool LocalPoint(PointerEventData data, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out local) &&
            _fitColumns > 0 && TileSize > 0f;
    }
}
