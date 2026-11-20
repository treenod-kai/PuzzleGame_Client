using System;

namespace LobbyDefense.Model
{
    /// <summary>한 번에 강화할 수 있는 카드 능력치다.</summary>
    public enum DefenseAttribute { Attack, Health, Defense }

    /// <summary>유저가 보유한 카드 한 장의 태생 점수, 고유 등급과 개별 강화값이다.</summary>
    [Serializable]
    public sealed class OwnedDefenseCard
    {
        public string Id;
        public string TypeId;
        public DefenseStats Roll;
        public DefenseStats Upgrade;
        public int Grade;
        public int ReadyWave;
        public int ReserveHealth;

        /// <summary><paramref name="rules"/>에 존재하는 카드인지, 저장한 태생 점수·등급·강화값이 유효한지 확인한다.</summary>
        internal bool IsValid(DefenseRules rules) => !string.IsNullOrWhiteSpace(Id) && rules.Find(TypeId) != null &&
            Roll.IsValid && Roll.Attack <= 10 && Roll.Health <= 10 && Roll.Defense <= 10 &&
            Grade >= 1 && Grade <= 4 && (Grade == 4) == (Roll.Attack + Roll.Health + Roll.Defense == 30) &&
            Upgrade.IsValid && ReadyWave >= 0 && ReserveHealth >= 0;

        /// <summary><paramref name="definition"/>의 기본값과 보정치에 이 카드의 강화분을 더한 새 전투 유닛을 만든다.</summary>
        public DefenseUnit CreateUnit(DefenseUnitDefinition definition)
        {
            checked
            {
                return new DefenseUnit(Id, TypeId, definition.Base.Attack + Roll.Attack * definition.PerPoint.Attack + Upgrade.Attack,
                    definition.Base.Health + Roll.Health * definition.PerPoint.Health + Upgrade.Health,
                    definition.Base.Defense + Roll.Defense * definition.PerPoint.Defense + Upgrade.Defense);
            }
        }

        /// <summary><paramref name="definition"/>으로 최종 수치 범위를 확인하고 <paramref name="stat"/> 하나의 강화값을 올린다.</summary>
        internal bool TryUpgrade(DefenseUnitDefinition definition, DefenseAttribute stat)
        {
            DefenseUnit unit = CreateUnit(definition);
            switch (stat)
            {
                case DefenseAttribute.Attack when unit.AttackPower < int.MaxValue:
                    Upgrade.Attack++;
                    break;
                case DefenseAttribute.Health when unit.MaxHealth < int.MaxValue:
                    Upgrade.Health++;
                    break;
                case DefenseAttribute.Defense when unit.DefensePower < int.MaxValue:
                    Upgrade.Defense++;
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
