using System;

namespace LobbyDefense.Model
{
    /// <summary>완료한 틱의 방어 세션 전투 상태를 보관한다. 보드 거리표는 복원 시 다시 계산한다.</summary>
    [Serializable]
    public sealed class DefenseSessionState
    {
        public long Tick;
        public int Wave;
        public int SpawnedThisWave;
        public int NextSpawnNumber;
        public long NextNormalSpawnTick;
        public long NextGhostSpawnTick;
        public long NextWaveTick;
        public DefenseDefenderState[] Defenders;
        public DefenseEnemyState[] Enemies;
        public DefenseTrapState[] Traps;
    }

    /// <summary>배치된 보유 카드의 당시 능력치와 체력, 공격 대기 시간을 보관한다.</summary>
    [Serializable]
    public sealed class DefenseDefenderState
    {
        public string CardInstanceId;
        public string TypeId;
        public int AttackPower;
        public int MaxHealth;
        public int DefensePower;
        public int CurrentHealth;
        public int Cell;
        public int Range;
        public int AttackInterval;
        public long NextAttackTick;
    }

    /// <summary>침공 몬스터의 소환 번호와 이동 중 예약, 마비와 공격 상태를 보관한다.</summary>
    [Serializable]
    public sealed class DefenseEnemyState
    {
        public int SpawnNumber;
        public bool IsGhost;
        public int CurrentHealth;
        public int Cell;
        public int NextCell;
        public long ArrivalTick;
        public long NextAttackTick;
        public long StunUntilTick;
    }

    /// <summary>트랩의 설치 칸과 다음 충전 시각을 보관한다.</summary>
    [Serializable]
    public sealed class DefenseTrapState
    {
        public int Cell;
        public long ReadyTick;
    }

    /// <summary>전투 시계와 개체를 저장하거나 빈 보드에 복원한다.</summary>
    public sealed partial class DefenseSession
    {
        /// <summary>완료한 틱과 현재 편성을 복사해 저장 후에도 독립적으로 보관할 수 있게 한다.</summary>
        public DefenseSessionState Capture()
        {
            DefenseSessionState state = new DefenseSessionState
            {
                Tick = Tick,
                Wave = Wave,
                SpawnedThisWave = _spawnedThisWave,
                NextSpawnNumber = _nextSpawnNumber,
                NextNormalSpawnTick = NextNormalSpawnTick,
                NextGhostSpawnTick = NextGhostSpawnTick,
                NextWaveTick = NextWaveTick,
                Defenders = new DefenseDefenderState[_defenders.Count],
                Enemies = new DefenseEnemyState[_enemies.Count],
                Traps = new DefenseTrapState[_traps.Count]
            };
            for (int i = 0; i < _defenders.Count; i++)
            {
                DeployedDefender defender = _defenders[i];
                DefenseUnit unit = defender.Unit;
                state.Defenders[i] = new DefenseDefenderState
                {
                    CardInstanceId = unit.CardInstanceId, TypeId = unit.UnitTypeId,
                    AttackPower = unit.AttackPower, MaxHealth = unit.MaxHealth, DefensePower = unit.DefensePower,
                    CurrentHealth = unit.CurrentHealth, Cell = defender.Cell, Range = defender.Range,
                    AttackInterval = defender.AttackInterval, NextAttackTick = defender.NextAttackTick
                };
            }
            for (int i = 0; i < _enemies.Count; i++)
            {
                InvadingMonster enemy = _enemies[i];
                state.Enemies[i] = new DefenseEnemyState
                {
                    SpawnNumber = enemy.Unit.SpawnNumber, IsGhost = enemy.IsGhost, CurrentHealth = enemy.Unit.CurrentHealth,
                    Cell = enemy.Cell, NextCell = enemy.NextCell, ArrivalTick = enemy.ArrivalTick,
                    NextAttackTick = enemy.NextAttackTick, StunUntilTick = enemy.StunUntilTick
                };
            }
            for (int i = 0; i < _traps.Count; i++)
            {
                state.Traps[i] = new DefenseTrapState { Cell = _traps[i].Cell, ReadyTick = _traps[i].ReadyTick };
            }
            return state;
        }

        /// <summary>새 전장 <paramref name="board"/>에 남는 칸으로 배치와 이동 예약을 옮기고 웨이브 시계와 개체 순서를 보존한다.</summary>
        internal DefenseSessionState CaptureForBoard(DefenseBoard board)
        {
            DefenseSessionState state = Capture();
            foreach (DefenseDefenderState defender in state.Defenders)
            {
                defender.Cell = RemapCell(defender.Cell, board);
            }
            state.Defenders = Array.FindAll(state.Defenders, defender => board.CanPlace(defender.Cell));
            foreach (DefenseTrapState trap in state.Traps)
            {
                trap.Cell = RemapCell(trap.Cell, board);
            }
            state.Traps = Array.FindAll(state.Traps, trap => board.CanPlace(trap.Cell));
            foreach (DefenseEnemyState enemy in state.Enemies)
            {
                enemy.Cell = RemapCell(enemy.Cell, board);
                enemy.NextCell = RemapCell(enemy.NextCell, board);
                if (enemy.NextCell < 0)
                {
                    enemy.ArrivalTick = 0;
                }
            }
            state.Enemies = Array.FindAll(state.Enemies, enemy => enemy.Cell >= 0 && enemy.Cell != board.Exit);
            return state;
        }

        /// <summary>기존 전장의 <paramref name="cell"/>과 행·열이 같은 <paramref name="board"/>의 칸 번호를 반환하고 범위 밖이면 -1을 반환한다.</summary>
        private int RemapCell(int cell, DefenseBoard board)
        {
            if (cell < 0)
            {
                return -1;
            }
            int column = cell % Board.Width;
            int row = cell / Board.Width;
            return column < board.Width && row < board.Height ? row * board.Width + column : -1;
        }

        /// <summary>빈 <paramref name="board"/>에 같은 버전의 <paramref name="rules"/>와 저장값 <paramref name="state"/>를 사용해 복원한다.</summary>
        public static DefenseSession Restore(DefenseBoard board, DefenseCombatRules rules, DefenseSessionState state)
        {
            DefenseSession session = new DefenseSession(board, rules);
            session.ValidateClocks(state);
            try
            {
                session.RestoreDefenders(state);
                session.RestoreTraps(state);
                session.RestoreEnemies(state);
            }
            catch
            {
                for (int i = session._defenders.Count - 1; i >= 0; i--)
                {
                    session.RemoveDefenderAt(i);
                }
                throw;
            }
            session.Tick = state.Tick;
            session.Wave = state.Wave;
            session._spawnedThisWave = state.SpawnedThisWave;
            session._nextSpawnNumber = state.NextSpawnNumber;
            session.NextNormalSpawnTick = state.NextNormalSpawnTick;
            session.NextGhostSpawnTick = state.NextGhostSpawnTick;
            session.NextWaveTick = state.NextWaveTick;
            return session;
        }

        /// <summary>저장값 <paramref name="state"/>의 웨이브 진행 단계와 소환 시계가 서로 일치하는지 검사한다.</summary>
        private void ValidateClocks(DefenseSessionState state)
        {
            if (state == null || state.Defenders == null || state.Enemies == null || state.Traps == null
                || state.Tick < 0 || state.Tick > long.MaxValue - int.MaxValue || state.Wave < 0
                || state.NextSpawnNumber < state.SpawnedThisWave || state.SpawnedThisWave < 0 || state.SpawnedThisWave > _rules.WaveSize)
            {
                throw new ArgumentException("저장된 방어 세션의 목록이나 시계가 유효하지 않습니다.");
            }
            if (state.Wave == 0)
            {
                if (state.Tick != 0 || state.SpawnedThisWave != 0 || state.NextSpawnNumber != 0
                    || state.NextNormalSpawnTick != -1 || state.NextGhostSpawnTick != -1 || state.NextWaveTick != -1
                    || state.Defenders.Length != 0 || state.Enemies.Length != 0)
                {
                    throw new ArgumentException("첫 배치 이전의 소환 상태가 유효하지 않습니다.");
                }
                return;
            }
            bool needsNormal = state.SpawnedThisWave < _rules.WaveSize;
            bool validNormal = needsNormal
                ? state.NextNormalSpawnTick > state.Tick && state.NextNormalSpawnTick <= state.Tick + _rules.NormalSpawnTicks
                : state.NextNormalSpawnTick == -1;
            bool validPreparation = state.NextWaveTick == -1 || (!needsNormal && state.NextWaveTick > state.Tick
                && state.NextWaveTick <= state.Tick + _rules.PreparationTicks);
            if (!validNormal || !validPreparation || state.NextGhostSpawnTick <= state.Tick
                || state.NextGhostSpawnTick > state.Tick + _rules.GhostSpawnTicks)
            {
                throw new ArgumentException("저장된 웨이브와 일반·유령 소환 시각이 일치하지 않습니다.");
            }
        }

        /// <summary><paramref name="state"/>의 카드 배치, 최종 능력치와 남은 체력을 복구한다.</summary>
        private void RestoreDefenders(DefenseSessionState state)
        {
            for (int i = 0; i < state.Defenders.Length; i++)
            {
                DefenseDefenderState saved = state.Defenders[i];
                if (saved == null || saved.CurrentHealth <= 0 || saved.CurrentHealth > saved.MaxHealth
                    || saved.NextAttackTick < 0 || saved.NextAttackTick > state.Tick + saved.AttackInterval)
                {
                    throw new ArgumentException("저장된 방어 유닛의 체력·공격 시각이 유효하지 않습니다.");
                }
                DefenseUnit unit = new DefenseUnit(saved.CardInstanceId, saved.TypeId, saved.AttackPower, saved.MaxHealth, saved.DefensePower);
                unit.ApplyDamage(unit.MaxHealth - saved.CurrentHealth);
                if (!TryPlace(unit, saved.Cell, saved.Range, saved.AttackInterval))
                {
                    throw new ArgumentException("저장된 보유 카드나 배치 칸이 중복되었거나 유효하지 않습니다.");
                }
                _defendersByCell[saved.Cell].NextAttackTick = saved.NextAttackTick;
            }
        }

        /// <summary><paramref name="state"/>의 통행 가능한 트랩과 재충전 시각을 복구한다.</summary>
        private void RestoreTraps(DefenseSessionState state)
        {
            for (int i = 0; i < state.Traps.Length; i++)
            {
                DefenseTrapState saved = state.Traps[i];
                if (saved == null || saved.ReadyTick < 0 || saved.ReadyTick > state.Tick + _rules.TrapRechargeTicks
                    || !TryPlaceTrap(saved.Cell))
                {
                    throw new ArgumentException("저장된 트랩 칸이나 충전 시각이 유효하지 않습니다.");
                }
                _trapsByCell[saved.Cell].ReadyTick = saved.ReadyTick;
            }
        }

        /// <summary><paramref name="state"/>의 소환 순서, 이동 예약과 마비 시계를 보존해 침공 개체를 복구한다.</summary>
        private void RestoreEnemies(DefenseSessionState state)
        {
            int previousNumber = -1;
            int normalCount = 0;
            for (int i = 0; i < state.Enemies.Length; i++)
            {
                DefenseEnemyState saved = state.Enemies[i];
                if (saved == null || saved.SpawnNumber <= previousNumber || saved.SpawnNumber >= state.NextSpawnNumber
                    || !Board.Contains(saved.Cell) || Board.IsOccupied(saved.Cell) || saved.Cell == Board.Exit)
                {
                    throw new ArgumentException("저장된 침공 몬스터의 소환 순서나 위치가 유효하지 않습니다.");
                }
                DefenseMonsterRules rules = saved.IsGhost ? _rules.Ghost : _rules.Normal;
                bool validMove = saved.NextCell == -1 && saved.ArrivalTick == 0;
                if (saved.NextCell >= 0)
                {
                    validMove = Board.Contains(saved.NextCell) && !Board.IsOccupied(saved.NextCell)
                        && Board.GetManhattanDistance(saved.Cell, saved.NextCell) == 1 && saved.ArrivalTick > state.Tick
                        && saved.ArrivalTick <= state.Tick + rules.MoveTicks && saved.StunUntilTick <= state.Tick;
                }
                if (!validMove || saved.CurrentHealth <= 0 || saved.CurrentHealth > rules.MaxHealth
                    || saved.NextAttackTick < 0 || saved.NextAttackTick > state.Tick + rules.AttackTicks
                    || saved.StunUntilTick < 0 || saved.StunUntilTick > state.Tick + _rules.TrapStunTicks
                    || (saved.IsGhost && saved.StunUntilTick != 0))
                {
                    throw new ArgumentException("저장된 침공 몬스터의 체력, 이동·공격·마비 시각이 유효하지 않습니다.");
                }
                InvadingMonster enemy = new InvadingMonster(saved.SpawnNumber, saved.Cell, saved.IsGhost, rules);
                enemy.Unit.ApplyDamage(rules.MaxHealth - saved.CurrentHealth);
                enemy.NextCell = saved.NextCell;
                enemy.ArrivalTick = saved.ArrivalTick;
                enemy.NextAttackTick = saved.NextAttackTick;
                enemy.StunUntilTick = saved.StunUntilTick;
                _enemies.Add(enemy);
                previousNumber = saved.SpawnNumber;
                if (!saved.IsGhost)
                {
                    normalCount++;
                }
            }
            if (normalCount > state.SpawnedThisWave || (normalCount > 0 && state.NextWaveTick >= 0))
            {
                throw new ArgumentException("저장된 일반 몬스터 수와 웨이브 상태가 일치하지 않습니다.");
            }
        }
    }
}
