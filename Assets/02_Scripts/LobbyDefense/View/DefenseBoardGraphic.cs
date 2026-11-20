using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>전체 지도와 확대 지도에서 칸·경로·사거리를 한 메시로 그리고 포인터를 보드 좌표로 바꾼다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DefenseBoardGraphic : MaskableGraphic, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler
    {
        private static readonly Color Empty = new Color32(29, 67, 71, 255);
        private static readonly Color Alternate = new Color32(33, 73, 76, 255);
        private static readonly Color Route = new Color32(81, 136, 127, 255);
        private static readonly Color Preview = new Color32(170, 210, 141, 255);
        private static readonly Color Gate = new Color32(242, 183, 90, 255);
        private static readonly Color Invalid = new Color32(228, 116, 94, 255);
        private int _width;
        private int _height;
        private int _entrance;
        private int _exit;
        private int _first;
        private int _columns;
        private int _windowFirst;
        private int _windowWidth;
        private int _candidate = -1;
        private int _range;
        private bool _canPlace;
        private bool[] _route;
        private bool[] _preview;
        private Action<int> _select;
        private Action<int> _pan;
        private int? _dragPointerId;
        private int _dragFirst;
        private Vector2 _dragStart;

        internal float TileSize => Mathf.Min(rectTransform.rect.width / _columns, rectTransform.rect.height / _height);

        /// <summary>보드 폭 <paramref name="width"/>, 높이 <paramref name="height"/>, 양쪽 문 <paramref name="entrance"/>·<paramref name="exit"/>와
        /// 표시 열 수 <paramref name="columns"/>, 선택 <paramref name="select"/>·가로 이동 <paramref name="pan"/> 콜백을 연결한다.</summary>
        internal void Initialize(int width, int height, int entrance, int exit, int columns, Action<int> select, Action<int> pan)
        {
            _width = width;
            _height = height;
            _entrance = entrance;
            _exit = exit;
            _columns = columns;
            _select = select;
            _pan = pan;
        }

        /// <summary>표시 시작 열 <paramref name="first"/> 및 전체 지도에 그릴 확대 창의 시작 <paramref name="windowFirst"/>와 폭 <paramref name="windowWidth"/>를 반영한다.</summary>
        internal void SetWindow(int first, int windowFirst, int windowWidth)
        {
            _first = first;
            _windowFirst = windowFirst;
            _windowWidth = windowWidth;
            SetVerticesDirty();
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
            Vector2 origin = rectTransform.rect.center;
            return origin + new Vector2((column - _first + 0.5f - _columns * 0.5f) * tile, (_height * 0.5f - row - 0.5f) * tile);
        }

        /// <summary>소수 열 <paramref name="column"/>이 현재 확대 창 안에 있는지 확인한다.</summary>
        internal bool Shows(float column) => column >= _first && column <= _first + _columns - 1;

        /// <summary>메시 작성기 <paramref name="vh"/>에 칸과 선택 창 테두리를 추가한다.</summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_width == 0 || TileSize <= 0f)
            {
                return;
            }
            float tile = TileSize;
            float half = tile * (_windowWidth > 0 ? 0.5f : 0.47f);
            Vector2 origin = CellCenter(_first, 0);
            for (int row = 0; row < _height; row++)
            {
                for (int column = _first; column < _first + _columns; column++)
                {
                    int cell = row * _width + column;
                    Vector2 p = origin + new Vector2(column - _first, -row) * tile;
                    Color tint = (column + row) % 2 == 0 ? Empty : Alternate;
                    if (_candidate >= 0 && _windowWidth == 0 &&
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
                    if (cell == _entrance || cell == _exit)
                    {
                        tint = Gate;
                    }
                    if (cell == _candidate)
                    {
                        tint = _canPlace ? Color.white : Invalid;
                    }
                    Quad(vh, p.x - half, p.y - half, p.x + half, p.y + half, tint);
                }
            }
            if (_windowWidth > 0)
            {
                Vector2 left = CellCenter(_windowFirst, 0) + new Vector2(-tile, tile) * 0.5f;
                Vector2 right = CellCenter(_windowFirst + _windowWidth - 1, _height - 1) + new Vector2(tile, -tile) * 0.5f;
                float line = Mathf.Min(tile, 2.5f);
                Quad(vh, left.x, left.y - line, right.x, left.y, Gate);
                Quad(vh, left.x, right.y, right.x, right.y + line, Gate);
                Quad(vh, left.x, right.y, left.x + line, left.y, Gate);
                Quad(vh, right.x - line, right.y, right.x, left.y, Gate);
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
            if (!eventData.eligibleForClick || eventData.button != PointerEventData.InputButton.Left || !LocalPoint(eventData, out Vector2 local))
            {
                return;
            }
            float tile = TileSize;
            Vector2 first = CellCenter(_first, 0);
            int column = Mathf.FloorToInt((local.x - first.x) / tile + 0.5f);
            int row = Mathf.FloorToInt((first.y - local.y) / tile + 0.5f);
            if (column >= 0 && column < _columns && row >= 0 && row < _height)
            {
                _select(row * _width + _first + column);
            }
        }

        /// <summary>지도에서 누른 <paramref name="eventData"/>의 포인터에 드래그를 맡긴다.</summary>
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _dragPointerId = eventData.pointerId;
            }
        }

        /// <summary>탭이나 팝업이 부모 입력 그룹을 전환하면 이미 누르고 있던 드래그를 취소한다.</summary>
        protected override void OnCanvasGroupChanged() => _dragPointerId = null;

        /// <summary>드래그 시작 <paramref name="eventData"/>의 로컬 위치와 시작 열을 보존한다.</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && _dragPointerId == eventData.pointerId)
            {
                eventData.eligibleForClick = false;
                _dragFirst = _first;
                LocalPoint(eventData, out _dragStart);
            }
        }

        /// <summary>드래그 중인 <paramref name="eventData"/>의 가로 이동량을 열 단위로 전달한다.</summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _dragPointerId != eventData.pointerId ||
                !LocalPoint(eventData, out Vector2 local))
            {
                return;
            }
            int delta = Mathf.RoundToInt((local.x - _dragStart.x) / TileSize);
            if (_windowWidth > 0)
            {
                int pointed = Mathf.FloorToInt((local.x - CellCenter(0, 0).x) / TileSize + 0.5f);
                _pan(pointed - _windowWidth / 2);
            }
            else
            {
                _pan(_dragFirst - delta);
            }
        }

        /// <summary>입력 <paramref name="data"/>의 위치를 지도 안의 로컬 좌표 <paramref name="local"/>로 바꾼다.</summary>
        private bool LocalPoint(PointerEventData data, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out local) &&
                _columns > 0 && TileSize > 0f;
        }
    }
}
