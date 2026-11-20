using System.Collections.Generic;
using LobbyDefense.Model;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>지도 하나의 유닛·트랩 표시 객체를 재사용하고 확대 지도에서 이동 보간과 잔여 체력을 표시한다.</summary>
    internal sealed class DefenseUnitMarkers
    {
        /// <summary>지도에 표시할 스프라이트와 체력 막대를 함께 보관한다.</summary>
        private sealed class Marker
        {
            public Image Icon;
            public RectTransform Health;
            public RectTransform Fill;
            public Vector2 LastPosition;
            public float LastSize;
        }

        private static readonly Color DefenderColor = new Color32(116, 230, 192, 255);
        private static readonly Color EnemyColor = new Color32(246, 157, 126, 255);
        private static readonly Color TrapColor = new Color32(184, 159, 241, 255);
        private readonly DefenseBoardGraphic _grid;
        private readonly LobbyDefenseView _view;
        private readonly int _width;
        private readonly bool _overview;
        private readonly List<Marker> _markers = new List<Marker>();
        private int _used;
        private float _tileSize;
        private Vector2 _firstCellCenter;

        /// <summary>지도 <paramref name="grid"/>, 이미지 공급 뷰 <paramref name="view"/>, 보드 폭 <paramref name="width"/>, 전체 보기 여부 <paramref name="overview"/>를 연결한다.</summary>
        internal DefenseUnitMarkers(DefenseBoardGraphic grid, LobbyDefenseView view, int width, bool overview)
        {
            _grid = grid;
            _view = view;
            _width = width;
            _overview = overview;
        }

        /// <summary><paramref name="session"/>에 있는 개체를 <paramref name="tickFraction"/>만큼 이동 보간하여 순서대로 그린다.</summary>
        internal void Render(DefenseSession session, float tickFraction)
        {
            _used = 0;
            _tileSize = _grid.TileSize;
            _firstCellCenter = _grid.CellCenter(0, 0);
            for (int i = 0; i < session.Traps.Count; i++)
            {
                DefenseTrap trap = session.Traps[i];
                Color tint = trap.ReadyTick <= session.Tick ? Color.white : Color.gray;
                Show(trap.Cell % _width, trap.Cell / _width, "Trap", null, TrapColor, tint, 1f);
            }
            for (int i = 0; i < session.Defenders.Count; i++)
            {
                DeployedDefender defender = session.Defenders[i];
                UnitBase unit = defender.Unit;
                Show(defender.Cell % _width, defender.Cell / _width, "Defenders", unit.UnitTypeId,
                    DefenderColor, Color.white, (float)unit.CurrentHealth / unit.MaxHealth);
            }
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                InvadingMonster enemy = session.Enemies[i];
                float column = enemy.Cell % _width;
                float row = enemy.Cell / _width;
                if (enemy.NextCell >= 0 && enemy.MoveTicks > 0)
                {
                    float progress = 1f - (enemy.ArrivalTick - session.Tick - tickFraction) / enemy.MoveTicks;
                    column = Mathf.Lerp(column, enemy.NextCell % _width, progress);
                    row = Mathf.Lerp(row, enemy.NextCell / _width, progress);
                }
                UnitBase unit = enemy.Unit;
                Show(column, row, "Monsters", unit.UnitTypeId, EnemyColor, Color.white, (float)unit.CurrentHealth / unit.MaxHealth);
            }
            for (int i = _used; i < _markers.Count && _markers[i].Icon.gameObject.activeSelf; i++)
            {
                _markers[i].Icon.gameObject.SetActive(false);
            }
        }

        /// <summary>좌표 <paramref name="column"/>·<paramref name="row"/>에 종류 <paramref name="group"/>·<paramref name="id"/>를
        /// 대체색 <paramref name="fallback"/>, 상태색 <paramref name="tint"/>, 체력비 <paramref name="health"/>로 나타낸다.</summary>
        private void Show(float column, float row, string group, string id, Color fallback, Color tint, float health)
        {
            if (!_grid.Shows(column, row))
            {
                return;
            }
            if (_used == _markers.Count)
            {
                _markers.Add(CreateMarker());
            }
            Marker marker = _markers[_used++];
            Image image = marker.Icon;
            if (!image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(true);
            }
            Sprite sprite = null;
            if (!_overview)
            {
                sprite = _view.FindSprite(group, id);
            }
            image.sprite = sprite;
            image.color = sprite == null ? fallback * tint : tint;
            Vector2 position = _firstCellCenter + new Vector2(column, -row) * _tileSize;
            float size = _tileSize * (_overview ? 0.8f : 1f);
            if (marker.LastPosition != position)
            {
                marker.LastPosition = position;
                image.rectTransform.anchoredPosition = position;
            }
            if (marker.LastSize != size)
            {
                marker.LastSize = size;
                image.rectTransform.sizeDelta = Vector2.one * size;
            }
            if (marker.Health != null)
            {
                bool damaged = health < 1f;
                if (marker.Health.gameObject.activeSelf != damaged)
                {
                    marker.Health.gameObject.SetActive(damaged);
                }
                if (damaged && marker.Fill.anchorMax.x != health)
                {
                    marker.Fill.anchorMax = new Vector2(health, 1f);
                }
            }
        }

        /// <summary>지도 아래에 표시 한 개를 만들고 확대 지도에는 체력 막대를 붙인다.</summary>
        private Marker CreateMarker()
        {
            Marker marker = new Marker();
            RectTransform rect = LobbyDefenseView.Rect("Unit", _grid.transform, 0.5f, 0.5f, 0.5f, 0.5f);
            marker.Icon = rect.gameObject.AddComponent<Image>();
            marker.Icon.raycastTarget = false;
            marker.Icon.preserveAspect = true;
            if (!_overview)
            {
                marker.Health = LobbyDefenseView.Rect("Health", rect, 0.08f, 0f, 0.92f, 0.08f);
                LobbyDefenseView.Paint(marker.Health, new Color32(18, 33, 37, 255));
                marker.Fill = LobbyDefenseView.Rect("Fill", marker.Health, 0f, 0f, 1f, 1f);
                LobbyDefenseView.Paint(marker.Fill, new Color32(153, 230, 157, 255));
            }
            return marker;
        }
    }
}
