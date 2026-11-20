using System;
using System.Collections.Generic;
using LobbyDefense.Model;
using Puzzle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>지도 하나의 유닛·트랩 표시 객체를 재사용하고 확대 지도에서 이동·공격 동작과 잔여 체력을 표시한다.</summary>
    internal sealed class DefenseUnitMarkers
    {
        /// <summary>지도에 표시할 스프라이트와 체력 막대를 함께 보관한다.</summary>
        private sealed class Marker
        {
            public RectTransform Root;
            public Image Icon;
            public RectTransform Health;
            public RectTransform Fill;
            public Vector2 LastPosition;
            public float LastSize;
        }

        private const int FrameCount = 4;
        private const int IdleFps = 4;
        private const int ActionFps = 8;
        // 모션 프레임은 256픽셀 원화의 사방에 32픽셀 여백을 포함한다.
        private const float MotionScale = 320f / 256f;
        private static readonly string[,] Directions =
        {
            { "NorthWest", "North", "NorthEast" },
            { "West",      "East",  "East" },
            { "SouthWest", "South", "SouthEast" }
        };
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
                Marker marker = PlaceMarker(trap.Cell % _width, trap.Cell / _width, TrapColor * tint, 1f);
                if (marker != null)
                {
                    Sprite sprite = _view.FindSprite("Trap", null);
                    SetArt(marker, sprite, sprite == null ? TrapColor * tint : tint, false, 1f);
                }
            }
            for (int i = 0; i < session.Defenders.Count; i++)
            {
                DeployedDefender defender = session.Defenders[i];
                UnitBase unit = defender.Unit;
                Marker marker = PlaceMarker(defender.Cell % _width, defender.Cell / _width, DefenderColor,
                    (float)unit.CurrentHealth / unit.MaxHealth);
                if (marker != null)
                {
                    int attack = AttackFrame(session.Tick, defender.LastAttackTick, tickFraction);
                    int frame = attack >= 0 ? attack : LoopFrame(session.Tick, tickFraction, IdleFps);
                    bool targetOnRight = _view.FacesRight(defender);
                    DrawUnit(marker, unit.UnitTypeId, attack >= 0 ? "Attack" : "Idle", null, frame, targetOnRight);
                }
            }
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                InvadingMonster enemy = session.Enemies[i];
                Vector2 position = EnemyPosition(enemy, _width, session.Tick, tickFraction);
                Marker marker = PlaceMarker(position.x, position.y, EnemyColor, (float)enemy.Unit.CurrentHealth / enemy.Unit.MaxHealth);
                if (marker != null)
                {
                    DrawMonster(marker, session, enemy, tickFraction);
                }
            }
            for (int i = _used; i < _markers.Count && _markers[i].Root.gameObject.activeSelf; i++)
            {
                _markers[i].Root.gameObject.SetActive(false);
            }
        }

        /// <summary>보드 폭 <paramref name="width"/>에서 <paramref name="enemy"/>의 칸을 <paramref name="tick"/>과
        /// 보간 비율 <paramref name="tickFraction"/>에 맞춰 연속 열·행 좌표로 구한다.</summary>
        internal static Vector2 EnemyPosition(InvadingMonster enemy, int width, long tick, float tickFraction)
        {
            Vector2 position = new Vector2(enemy.Cell % width, enemy.Cell / width);
            if (enemy.NextCell < 0 || enemy.MoveTicks <= 0)
            {
                return position;
            }
            float progress = 1f - (enemy.ArrivalTick - tick - tickFraction) / enemy.MoveTicks;
            Vector2 next = new Vector2(enemy.NextCell % width, enemy.NextCell / width);
            return Vector2.Lerp(position, next, progress);
        }

        /// <summary><paramref name="session"/>의 <paramref name="enemy"/>를 공격 대상이나 이동 방향에 맞춰
        /// <paramref name="marker"/>에 표시한다. <paramref name="tickFraction"/>은 현재 틱 사이의 진행률이다.</summary>
        private void DrawMonster(Marker marker, DefenseSession session, InvadingMonster enemy, float tickFraction)
        {
            bool stunned = session.Tick < enemy.StunUntilTick;
            int attack = stunned ? -1 : AttackFrame(session.Tick, enemy.LastAttackTick, tickFraction);
            int target = attack >= 0 ? enemy.LastAttackTargetCell : enemy.NextCell;
            if (target < 0)
            {
                target = session.Board.GetNextCell(enemy.Cell);
            }
            int dx = target < 0 ? 0 : Math.Sign(target % _width - enemy.Cell % _width);
            int dy = target < 0 ? 0 : Math.Sign(target / _width - enemy.Cell / _width);
            string direction = Directions[dy + 1, dx + 1];
            int frame = attack;
            if (attack < 0)
            {
                frame = !stunned && enemy.NextCell >= 0 ? LoopFrame(session.Tick, tickFraction, ActionFps) : 0;
            }
            DrawUnit(marker, enemy.Unit.UnitTypeId, attack >= 0 ? "Attack" : "Walk", direction, frame, dx < 0);
        }

        /// <summary>마지막 타격 <paramref name="start"/>부터 현재 <paramref name="tick"/>과 <paramref name="fraction"/>까지
        /// 진행한 공격 프레임을 구한다. 재생 구간 밖에서는 -1이다.</summary>
        private static int AttackFrame(long tick, long start, float fraction)
        {
            if (start < 0 || start > tick)
            {
                return -1;
            }
            double frame = (tick - start + (double)fraction) * ActionFps / GameSpec.LogicFramesPerSecond;
            return frame < FrameCount ? (int)frame : -1;
        }

        /// <summary>장시간 실행해도 <paramref name="tick"/>과 틱 사이 비율 <paramref name="fraction"/>을
        /// <paramref name="fps"/>의 반복 프레임으로 정밀하게 환산한다.</summary>
        private static int LoopFrame(long tick, float fraction, int fps) =>
            (int)((tick % GameSpec.LogicFramesPerSecond + fraction) * fps / GameSpec.LogicFramesPerSecond) % FrameCount;

        /// <summary><paramref name="marker"/>에 <paramref name="id"/>의 <paramref name="motion"/>·<paramref name="direction"/>·
        /// <paramref name="frame"/>을 표시한다. 수비 원화와 정적 대체 그림은 <paramref name="mirrorBase"/>를 따른다.</summary>
        private void DrawUnit(Marker marker, string id, string motion, string direction, int frame, bool mirrorBase)
        {
            bool defender = direction == null;
            Sprite sprite = _view.Motions.FindFrame(id, motion, direction, frame, out bool clipFlip);
            bool animated = sprite != null;
            if (!animated)
            {
                sprite = _view.FindSprite(defender ? "Defenders" : "Monsters", id);
            }
            Color fallback = defender ? DefenderColor : EnemyColor;
            bool flipX = animated && !defender ? clipFlip : mirrorBase;
            SetArt(marker, sprite, sprite == null ? fallback : Color.white, flipX, animated ? MotionScale : 1f);
        }

        /// <summary><paramref name="marker"/>의 그림 <paramref name="sprite"/>에 <paramref name="tint"/>·
        /// <paramref name="flipX"/>·<paramref name="scale"/>을 적용하고 체력 막대의 방향은 유지한다.</summary>
        private static void SetArt(Marker marker, Sprite sprite, Color tint, bool flipX, float scale)
        {
            Image image = marker.Icon;
            image.sprite = sprite;
            image.color = tint;
            Vector3 artScale = new Vector3(flipX ? -scale : scale, scale, 1f);
            if (image.rectTransform.localScale != artScale)
            {
                image.rectTransform.localScale = artScale;
            }
        }

        /// <summary><paramref name="column"/>·<paramref name="row"/>의 위치와 <paramref name="health"/>를 갱신하고
        /// 상세 그림이 필요할 때 마커를 반환한다. 전체 지도는 <paramref name="overviewColor"/> 점으로 표시한다.</summary>
        private Marker PlaceMarker(float column, float row, Color overviewColor, float health)
        {
            if (!_grid.Shows(column, row))
            {
                return null;
            }
            if (_used == _markers.Count)
            {
                _markers.Add(CreateMarker());
            }
            Marker marker = _markers[_used++];
            if (!marker.Root.gameObject.activeSelf)
            {
                marker.Root.gameObject.SetActive(true);
            }
            Vector2 position = _firstCellCenter + new Vector2(column, -row) * _tileSize;
            float size = _tileSize * (_overview ? 0.8f : 1f);
            if (marker.LastPosition != position)
            {
                marker.LastPosition = position;
                marker.Root.anchoredPosition = position;
            }
            if (marker.LastSize != size)
            {
                marker.LastSize = size;
                marker.Root.sizeDelta = Vector2.one * size;
            }
            if (_overview)
            {
                marker.Icon.color = overviewColor;
                return null;
            }
            bool damaged = health < 1f;
            if (marker.Health.gameObject.activeSelf != damaged)
            {
                marker.Health.gameObject.SetActive(damaged);
            }
            if (damaged && marker.Fill.anchorMax.x != health)
            {
                marker.Fill.anchorMax = new Vector2(health, 1f);
            }
            return marker;
        }

        /// <summary>지도 아래에 표시 한 개를 만들고 확대 지도에는 체력 막대를 붙인다.</summary>
        private Marker CreateMarker()
        {
            Marker marker = new Marker();
            RectTransform rect = LobbyDefenseView.Rect("Unit", _grid.transform, 0.5f, 0.5f, 0.5f, 0.5f);
            marker.Root = rect;
            RectTransform art = _overview ? rect : LobbyDefenseView.Rect("Art", rect, 0f, 0f, 1f, 1f);
            marker.Icon = art.gameObject.AddComponent<Image>();
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
