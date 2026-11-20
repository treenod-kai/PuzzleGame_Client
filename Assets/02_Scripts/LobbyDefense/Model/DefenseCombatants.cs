namespace LobbyDefense.Model
{
    /// <summary>보드에 배치한 카드의 전투 개체와 현재 칸, 사거리, 공격 시점을 보관한다.</summary>
    public sealed class DeployedDefender
    {
        public DefenseUnit Unit { get; internal set; }
        public int Cell { get; internal set; }
        public int Range { get; }
        public int AttackInterval { get; }
        public long NextAttackTick { get; internal set; }
        /// <summary>마지막으로 피해를 준 틱이다. 배치나 복원 후 첫 공격 전에는 -1이다.</summary>
        public long LastAttackTick { get; internal set; } = -1;
        /// <summary>마지막 공격 당시 몬스터가 있던 칸이다. 첫 공격 전에는 -1이다.</summary>
        public int LastAttackTargetCell { get; internal set; } = -1;
        internal long LastMissedTargetTick { get; set; } = -1;

        /// <summary>배치할 <paramref name="unit"/>의 위치 <paramref name="cell"/>, 사거리 <paramref name="range"/>,
        /// 공격 주기 <paramref name="attackInterval"/>를 설정한다.</summary>
        internal DeployedDefender(DefenseUnit unit, int cell, int range, int attackInterval)
        {
            Unit = unit;
            Cell = cell;
            Range = range;
            AttackInterval = attackInterval;
        }
    }

    /// <summary>소환된 몬스터의 전투 개체와 이동 예약, 공격 및 마비 시계를 관리한다.</summary>
    public sealed class InvadingMonster
    {
        public AttackUnit Unit { get; }
        public int Cell { get; internal set; }
        public int NextCell { get; internal set; } = -1;
        public long ArrivalTick { get; internal set; }
        public long NextAttackTick { get; internal set; }
        /// <summary>마지막으로 방어 유닛에 피해를 준 틱이다. 소환이나 복원 후 첫 공격 전에는 -1이다.</summary>
        public long LastAttackTick { get; internal set; } = -1;
        /// <summary>마지막 공격 당시 방어 유닛이 있던 칸이다. 첫 공격 전에는 -1이다.</summary>
        public int LastAttackTargetCell { get; internal set; } = -1;
        public long StunUntilTick { get; internal set; }
        public int MoveTicks { get; }
        public int AttackTicks { get; }
        public bool IsGhost { get; }

        /// <summary>소환 번호 <paramref name="number"/>, 시작 칸 <paramref name="cell"/>, 유령 여부 <paramref name="isGhost"/>와
        /// 종류별 전투 규칙 <paramref name="rules"/>로 몬스터를 생성한다.</summary>
        internal InvadingMonster(int number, int cell, bool isGhost, DefenseMonsterRules rules)
        {
            Unit = new AttackUnit(number, rules.TypeId, rules.AttackPower, rules.MaxHealth, rules.DefensePower);
            Cell = cell;
            IsGhost = isGhost;
            MoveTicks = rules.MoveTicks;
            AttackTicks = rules.AttackTicks;
        }
    }

    /// <summary>통행 가능한 칸에 배치된 마비 트랩과 다음 충전 완료 틱이다.</summary>
    public sealed class DefenseTrap
    {
        public int Cell { get; }
        public long ReadyTick { get; internal set; }

        /// <summary>통행을 유지할 <paramref name="cell"/>에 충전된 트랩을 생성한다.</summary>
        internal DefenseTrap(int cell)
        {
            Cell = cell;
        }
    }
}
