using System;

namespace LobbyDefense.Model
{
    /// <summary>방어선을 침공하는 몬스터 한 기를 나타낸다. 소환 번호로 같은 종류의 개체를 구별한다.</summary>
    public class AttackUnit : UnitBase
    {
        public int SpawnNumber { get; }

        /// <summary>소환 번호 <paramref name="spawnNumber"/>와 종류 <paramref name="unitTypeId"/>로 몬스터를 생성하고
        /// 공격력 <paramref name="attackPower"/>, 최대 체력 <paramref name="maxHealth"/>, 방어력 <paramref name="defensePower"/>를 사용한다.</summary>
        public AttackUnit(int spawnNumber, string unitTypeId, int attackPower, int maxHealth, int defensePower)
            : base(unitTypeId, attackPower, maxHealth, defensePower)
        {
            if (spawnNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spawnNumber));
            }

            SpawnNumber = spawnNumber;
        }
    }
}
