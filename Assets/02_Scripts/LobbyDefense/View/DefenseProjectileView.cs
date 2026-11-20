using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LobbyDefense.Model;
using Puzzle.Core;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>수비 유닛이 실제로 발사한 투사체를 확대 지도에서 목표까지 이동시킨다.</summary>
    internal sealed class DefenseProjectileView
    {
        /// <summary>사수의 마지막 조준 방향을 탄이 사라진 뒤에도 유지한다.</summary>
        private sealed class Aim
        {
            public bool Right;
        }

        /// <summary>발사 순간의 위치와 표적을 보관해 처치나 회수 후에도 연출을 마친다.</summary>
        private struct Shot
        {
            public string UnitId;
            public Aim Aim;
            public long Tick;
            public Vector2 Origin;
            public Vector2 TargetPosition;
            public InvadingMonster Target;
        }

        private const float ReleaseSeconds = 0.25f;
        private const float FlightSeconds = 0.24f;
        private readonly DefenseBoardGraphic _grid;
        private readonly int _width;
        private readonly RectTransform _layer;
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private readonly List<Shot> _shots = new List<Shot>();
        private readonly ConditionalWeakTable<DeployedDefender, Aim> _aims = new ConditionalWeakTable<DeployedDefender, Aim>();
        private readonly List<Image> _images = new List<Image>();
        private DefenseSession _session;

        /// <summary>확대 지도 <paramref name="grid"/>에 보드 폭 <paramref name="width"/> 기준의 발사체 표시 영역을 만든다.</summary>
        internal DefenseProjectileView(DefenseBoardGraphic grid, int width)
        {
            _grid = grid;
            _width = width;
            _layer = LobbyDefenseView.Rect("Projectiles", grid.transform, 0f, 0f, 1f, 1f);
        }

        /// <summary>표시할 <paramref name="game"/>이 바뀌면 이전 탄을 비우고 현재 카드 규칙에 등록된 탄을 준비한다.</summary>
        internal void Bind(DefenseCampaign game)
        {
            Unsubscribe();
            _shots.Clear();
            foreach (Image image in _images)
            {
                image.enabled = false;
            }
            if (game == null)
            {
                return;
            }
            foreach (DefenseUnitDefinition unit in game.Rules.Units)
            {
                if (!string.IsNullOrEmpty(unit.AttackProjectileAddress) && !_sprites.ContainsKey(unit.Id))
                {
                    _sprites.Add(unit.Id, null);
                    AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<Sprite>
                    {
                        address = unit.AttackProjectileAddress,
                        successCallback = sprite => _sprites[unit.Id] = sprite
                    });
                }
            }
            _session = game.Session;
            _session.DefenderFired += OnFired;
        }

        /// <summary>현재 전투의 발사 알림을 해제한다.</summary>
        internal void Unsubscribe()
        {
            if (_session != null)
            {
                _session.DefenderFired -= OnFired;
                _session = null;
            }
        }

        /// <summary>수비병 <paramref name="defender"/>이 <paramref name="target"/>을 명중시킨 틱과 두 개체의 위치를 보관한다.</summary>
        private void OnFired(DeployedDefender defender, InvadingMonster target)
        {
            string id = defender.Unit.UnitTypeId;
            if (_sprites.ContainsKey(id))
            {
                if (!_aims.TryGetValue(defender, out Aim aim))
                {
                    aim = new Aim();
                    _aims.Add(defender, aim);
                }
                _shots.Add(new Shot
                {
                    UnitId = id,
                    Aim = aim,
                    Tick = defender.LastAttackTick,
                    Origin = new Vector2(defender.Cell % _width, defender.Cell / _width),
                    TargetPosition = DefenseUnitMarkers.EnemyPosition(target, _width, defender.LastAttackTick, 0f),
                    Target = target
                });
            }
        }

        /// <summary><paramref name="tick"/>과 <paramref name="fraction"/>의 표적 위치를 한 번 구해 사수와 탄이 같은 방향을 보게 한다.</summary>
        internal void UpdateTargets(long tick, float fraction)
        {
            for (int i = 0; i < _shots.Count; i++)
            {
                Shot shot = _shots[i];
                if (shot.Target.Unit.IsAlive)
                {
                    shot.TargetPosition = DefenseUnitMarkers.EnemyPosition(shot.Target, _width, tick, fraction);
                    _shots[i] = shot;
                }
                shot.Aim.Right = shot.TargetPosition.x > shot.Origin.x;
                if (shot.Target.Unit.IsAlive && Mathf.Approximately(shot.TargetPosition.x, shot.Origin.x) && shot.Target.NextCell >= 0)
                {
                    shot.Aim.Right = shot.Target.NextCell % _width > shot.Origin.x;
                }
            }
        }

        /// <summary><paramref name="defender"/>의 마지막 조준 방향을 구한다. 탄이 없는 유닛은 마지막 타격 칸의 방향을 쓴다.</summary>
        internal bool FacesRight(DeployedDefender defender) =>
            _aims.TryGetValue(defender, out Aim aim) ? aim.Right :
                defender.LastAttackTargetCell >= 0 && defender.LastAttackTargetCell % _width > defender.Cell % _width;

        /// <summary>현재 <paramref name="tick"/>과 <paramref name="fraction"/>으로 접촉 프레임 이후 날아가는 탄만 그린다.</summary>
        internal void Render(long tick, float fraction)
        {
            int used = 0;
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                Shot shot = _shots[i];
                float elapsed = (tick - shot.Tick + fraction) / GameSpec.LogicFramesPerSecond - ReleaseSeconds;
                if (elapsed >= FlightSeconds)
                {
                    _shots.RemoveAt(i);
                    continue;
                }
                Sprite sprite = _sprites[shot.UnitId];
                if (elapsed < 0f || sprite == null)
                {
                    continue;
                }
                Vector2 target = shot.TargetPosition;
                Vector2 direction = (target - shot.Origin).normalized;
                Vector2 muzzle = Vector2.MoveTowards(shot.Origin, target, 0.37f);
                Vector2 position = Vector2.Lerp(muzzle, target, elapsed / FlightSeconds);
                if (_grid.Shows(position.x, position.y))
                {
                    Draw(used++, sprite, position, direction);
                }
            }
            for (int i = used; i < _images.Count && _images[i].enabled; i++)
            {
                _images[i].enabled = false;
            }
        }

        /// <summary>재사용할 <paramref name="index"/>번 탄에 <paramref name="sprite"/>를 입히고 보드 <paramref name="position"/>과 <paramref name="direction"/>을 반영한다.</summary>
        private void Draw(int index, Sprite sprite, Vector2 position, Vector2 direction)
        {
            if (index == 0 && _layer.GetSiblingIndex() < _grid.transform.childCount - 1)
            {
                _layer.SetAsLastSibling();
            }
            if (index == _images.Count)
            {
                RectTransform rect = LobbyDefenseView.Rect("Projectile", _layer, 0.5f, 0.5f, 0.5f, 0.5f);
                Image icon = rect.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                _images.Add(icon);
            }
            Image image = _images[index];
            image.sprite = sprite;
            image.rectTransform.anchoredPosition = _grid.CellCenter(position.x, position.y);
            image.rectTransform.sizeDelta = Vector2.one * (_grid.TileSize * 0.48f);
            float angle = Vector2.SignedAngle(Vector2.left, new Vector2(direction.x, -direction.y));
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            image.enabled = true;
        }
    }
}
