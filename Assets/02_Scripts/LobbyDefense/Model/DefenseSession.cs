using System;
using System.Collections.Generic;

namespace LobbyDefense.Model
{
    /// <summary>로비 방어선 하나의 배치, 침공 웨이브와 양쪽 진영의 고정 틱 전투를 진행한다.</summary>
    public sealed partial class DefenseSession
    {
        private readonly DefenseCombatRules _rules;
        private readonly List<DeployedDefender> _defenders = new List<DeployedDefender>();
        private readonly List<InvadingMonster> _enemies = new List<InvadingMonster>();
        private readonly List<DefenseTrap> _traps = new List<DefenseTrap>();
        private readonly List<string> _destroyedCards = new List<string>();
        private readonly DeployedDefender[] _defendersByCell;
        private readonly DefenseTrap[] _trapsByCell;
        private int _spawnedThisWave;
        private int _nextSpawnNumber;
        private long _lastNormalPositionTick;

        public DefenseBoard Board { get; }
        public IReadOnlyList<DeployedDefender> Defenders => _defenders;
        public IReadOnlyList<InvadingMonster> Enemies => _enemies;
        public IReadOnlyList<DefenseTrap> Traps => _traps;
        public IReadOnlyList<string> DestroyedCards => _destroyedCards;
        public long Tick { get; private set; }
        public int Wave { get; private set; }
        public bool HasStarted => Wave > 0;
        public bool IsPreparing => NextWaveTick >= 0;
        public long NextWaveTick { get; private set; } = -1;
        public long NextNormalSpawnTick { get; private set; } = -1;
        public long NextGhostSpawnTick { get; private set; } = -1;

        /// <summary>빈 <paramref name="board"/>와 전투 규칙 <paramref name="rules"/>를 받아 첫 배치를 기다린다.</summary>
        public DefenseSession(DefenseBoard board, DefenseCombatRules rules)
        {
            if (board == null || rules == null)
            {
                throw new ArgumentException("전투 보드와 규칙이 필요합니다.");
            }
            rules.Validate();
            for (int cell = 0; cell < board.CellCount; cell++)
            {
                if (board.IsOccupied(cell))
                {
                    throw new ArgumentException("새 세션은 빈 보드로 시작해야 합니다.", nameof(board));
                }
            }
            Board = board;
            _rules = rules;
            _defendersByCell = new DeployedDefender[board.CellCount];
            _trapsByCell = new DefenseTrap[board.CellCount];
        }

        /// <summary>보유 카드 <paramref name="cardInstanceId"/>가 현재 방어선을 점유하는지 확인한다.</summary>
        public bool IsCardDeployed(string cardInstanceId) => FindDefender(cardInstanceId) >= 0;

        /// <summary><paramref name="updated"/> 카드가 배치되어 있으면 누적 피해와 다음 공격 틱을 보존한 채 강화 능력치를 반영한다.</summary>
        internal void UpdateCardStats(DefenseUnit updated)
        {
            int index = FindDefender(updated.CardInstanceId);
            if (index >= 0)
            {
                DeployedDefender deployed = _defenders[index];
                updated.ApplyDamage(deployed.Unit.MaxHealth - deployed.Unit.CurrentHealth);
                deployed.Unit = updated;
            }
        }

        /// <summary>카드 ID <paramref name="cardInstanceId"/>로 배치 목록의 인덱스를 찾는다.</summary>
        private int FindDefender(string cardInstanceId)
        {
            for (int i = 0; i < _defenders.Count; i++)
            {
                if (_defenders[i].Unit.CardInstanceId == cardInstanceId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary><paramref name="cell"/>이 배치 가능하며 트랩이나 이동 중인 적의 현재·예약 칸과 겹치지 않는지 확인한다.</summary>
        public bool CanPlace(int cell)
        {
            if (!Board.CanPlace(cell) || _trapsByCell[cell] != null)
            {
                return false;
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Cell == cell || _enemies[i].NextCell == cell)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary><paramref name="cell"/>에 통행 가능한 트랩을 놓을 수 있는지 확인한다.</summary>
        public bool CanPlaceTrap(int cell) => CanPlace(cell);

        /// <summary>보유 유닛 <paramref name="unit"/>을 빈 <paramref name="cell"/>에 놓고 사거리 <paramref name="range"/>와
        /// 공격 주기 <paramref name="attackInterval"/>를 적용한다. 첫 배치에서 웨이브가 시작된다.</summary>
        public bool TryPlace(DefenseUnit unit, int cell, int range, int attackInterval)
        {
            if (unit == null || !unit.IsAlive || range < 0 || attackInterval <= 0 || !CanPlace(cell)
                || IsCardDeployed(unit.CardInstanceId))
            {
                return false;
            }
            DeployedDefender defender = new DeployedDefender(unit, cell, range, attackInterval);
            Board.TryPlace(cell);
            _defenders.Add(defender);
            _defendersByCell[cell] = defender;
            if (!HasStarted)
            {
                BeginWave();
                NextGhostSpawnTick = Tick + _rules.GhostSpawnTicks;
            }
            return true;
        }

        /// <summary><paramref name="cell"/>에 충전된 마비 트랩을 놓고 해당 칸의 통행을 유지한다.</summary>
        public bool TryPlaceTrap(int cell)
        {
            if (!CanPlaceTrap(cell))
            {
                return false;
            }
            DefenseTrap trap = new DefenseTrap(cell);
            _traps.Add(trap);
            _trapsByCell[cell] = trap;
            return true;
        }

        /// <summary>정비 중 보유 카드 <paramref name="cardInstanceId"/>를 기존 체력 그대로 빈 <paramref name="cell"/>로 이동한다.</summary>
        public bool TryMove(string cardInstanceId, int cell)
        {
            int index = FindDefender(cardInstanceId);
            if (!IsPreparing || index < 0 || !CanPlace(cell))
            {
                return false;
            }
            DeployedDefender defender = _defenders[index];
            Board.TryRemove(defender.Cell);
            _defendersByCell[defender.Cell] = null;
            Board.TryPlace(cell);
            defender.Cell = cell;
            defender.LastMissedTargetTick = -1;
            _defendersByCell[cell] = defender;
            return true;
        }

        /// <summary>정비 중 보유 카드 <paramref name="cardInstanceId"/>를 찾아 점유한 칸을 비운다.</summary>
        public bool TryRemove(string cardInstanceId)
        {
            int index = FindDefender(cardInstanceId);
            if (!IsPreparing || index < 0)
            {
                return false;
            }
            RemoveDefenderAt(index);
            return true;
        }

        /// <summary>배치 목록의 <paramref name="index"/>에 해당하는 유닛을 제거하고 경로를 개방한다.</summary>
        private void RemoveDefenderAt(int index)
        {
            int cell = _defenders[index].Cell;
            _defendersByCell[cell] = null;
            _defenders.RemoveAt(index);
            Board.TryRemove(cell);
        }

        /// <summary>정비 중 다음 일반 웨이브를 즉시 시작한다.</summary>
        public bool StartNextWave()
        {
            if (!IsPreparing)
            {
                return false;
            }
            BeginWave();
            return true;
        }

        /// <summary>웨이브 번호를 올리고 이번 웨이브의 첫 일반 몬스터 시각을 예약한다.</summary>
        private void BeginWave()
        {
            Wave++;
            _spawnedThisWave = 0;
            NextWaveTick = -1;
            NextNormalSpawnTick = Tick + _rules.NormalSpawnTicks;
        }

        /// <summary>논리 틱 하나에서 소환·이동·양쪽 공격·퇴장을 처리하고 이 틱의 일반 처치 골드를 반환한다.</summary>
        public int Step()
        {
            _destroyedCards.Clear();
            if (!HasStarted)
            {
                return 0;
            }
            Tick++;
            if (IsPreparing && Tick >= NextWaveTick)
            {
                BeginWave();
            }
            if (NextNormalSpawnTick >= 0 && Tick >= NextNormalSpawnTick)
            {
                Spawn(false);
                _lastNormalPositionTick = Tick;
                _spawnedThisWave++;
                NextNormalSpawnTick = _spawnedThisWave < _rules.WaveSize ? Tick + _rules.NormalSpawnTicks : -1;
            }
            if (Tick >= NextGhostSpawnTick)
            {
                Spawn(true);
                NextGhostSpawnTick = Tick + _rules.GhostSpawnTicks;
            }

            ArriveAndTriggerTraps();
            QueueDefenderAttacks();
            QueueSiegeAttacks();
            return ResolveCombatAndMove();
        }

        /// <summary>유령 여부 <paramref name="isGhost"/>에 맞는 몬스터를 입구에 소환한다.</summary>
        private void Spawn(bool isGhost)
        {
            DefenseMonsterRules rules = isGhost ? _rules.Ghost : _rules.Normal;
            _enemies.Add(new InvadingMonster(_nextSpawnNumber++, Board.Entrance, isGhost, rules));
        }

        /// <summary>도착한 개체부터 소환 번호 순서로 이동을 확정하고 일반 몬스터에게 충전된 트랩을 발동한다.</summary>
        private void ArriveAndTriggerTraps()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                InvadingMonster enemy = _enemies[i];
                if (enemy.NextCell < 0 || Tick < enemy.ArrivalTick)
                {
                    continue;
                }
                enemy.Cell = enemy.NextCell;
                enemy.NextCell = -1;
                enemy.ArrivalTick = 0;
                if (enemy.IsGhost)
                {
                    continue;
                }
                _lastNormalPositionTick = Tick;
                DefenseTrap trap = _trapsByCell[enemy.Cell];
                if (trap != null && Tick >= trap.ReadyTick)
                {
                    enemy.StunUntilTick = Tick + _rules.TrapStunTicks;
                    trap.ReadyTick = Tick + _rules.TrapRechargeTicks;
                }
            }
        }

        /// <summary>공격 가능한 배치 유닛별로 사거리 안의 우선 표적을 골라 이번 틱 피해를 누적한다.</summary>
        private void QueueDefenderAttacks()
        {
            for (int i = 0; i < _defenders.Count; i++)
            {
                DeployedDefender defender = _defenders[i];
                // 사거리 안이 비어 있었다면 사수의 위치나 표적이 될 몬스터의 위치가 바뀔 때 다시 찾는다.
                if (defender.Range == 0 || defender.Unit.AttackPower == 0 || Tick < defender.NextAttackTick ||
                    defender.LastMissedTargetTick == _lastNormalPositionTick)
                {
                    continue;
                }
                InvadingMonster target = FindTarget(defender);
                if (target != null)
                {
                    target.Unit.QueueHitFrom(defender.Unit);
                    defender.NextAttackTick = Tick + defender.AttackInterval;
                }
                else
                {
                    defender.LastMissedTargetTick = _lastNormalPositionTick;
                }
            }
        }

        /// <summary><paramref name="defender"/>의 사거리 안에서 출구까지 최단 거리, 같은 거리에서는 빠른 소환 번호를 우선한다.</summary>
        private InvadingMonster FindTarget(DeployedDefender defender)
        {
            InvadingMonster best = null;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                InvadingMonster enemy = _enemies[i];
                if (enemy.IsGhost || Board.GetManhattanDistance(defender.Cell, enemy.Cell) > defender.Range)
                {
                    continue;
                }
                int distance = Board.GetDistance(enemy.Cell);
                if (distance < 0)
                {
                    distance = int.MaxValue;
                }
                // 소환 순서로 순회하므로 같은 거리에는 최초 표적을 유지한다.
                if (best == null || distance < bestDistance)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>봉쇄된 적의 다음 경로 칸에 방어 유닛이 있을 때만 인접 공격을 예약한다.</summary>
        private void QueueSiegeAttacks()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                InvadingMonster enemy = _enemies[i];
                if (enemy.NextCell >= 0 || Tick < enemy.StunUntilTick || Tick < enemy.NextAttackTick)
                {
                    continue;
                }
                int next = Board.GetNextCell(enemy.Cell);
                if (next >= 0 && _defendersByCell[next] != null)
                {
                    _defendersByCell[next].Unit.QueueHitFrom(enemy.Unit);
                    enemy.NextAttackTick = Tick + enemy.AttackTicks;
                }
            }
        }

        /// <summary>같은 틱의 양쪽 공격을 적용한 다음 파괴 칸을 비우고 생존 몬스터의 다음 이동을 예약한다.</summary>
        private int ResolveCombatAndMove()
        {
            for (int i = _defenders.Count - 1; i >= 0; i--)
            {
                _defenders[i].Unit.ResolveHits();
                if (!_defenders[i].Unit.IsAlive)
                {
                    _destroyedCards.Add(_defenders[i].Unit.CardInstanceId);
                    RemoveDefenderAt(i);
                }
            }
            int gold = 0;
            bool hasNormal = false;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                InvadingMonster enemy = _enemies[i];
                enemy.Unit.ResolveHits();
                if (!enemy.Unit.IsAlive || enemy.Cell == Board.Exit)
                {
                    if (!enemy.Unit.IsAlive && !enemy.IsGhost)
                    {
                        gold += _rules.KillGold;
                    }
                    _enemies.RemoveAt(i);
                    continue;
                }
                if (!enemy.IsGhost)
                {
                    hasNormal = true;
                }
                if (enemy.NextCell < 0 && Tick >= enemy.StunUntilTick)
                {
                    int next = Board.GetNextCell(enemy.Cell);
                    if (next >= 0 && !Board.IsOccupied(next))
                    {
                        enemy.NextCell = next;
                        enemy.ArrivalTick = Tick + enemy.MoveTicks;
                    }
                }
            }
            if (!hasNormal && _spawnedThisWave == _rules.WaveSize && !IsPreparing)
            {
                NextWaveTick = Tick + _rules.PreparationTicks;
            }
            return gold;
        }
    }
}
