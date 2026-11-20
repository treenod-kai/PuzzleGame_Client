using System;

namespace LobbyDefense.Model
{
    /// <summary>로비 디펜스 유닛 한 기의 전투 능력치와 남은 체력을 관리한다.</summary>
    public abstract class UnitBase
    {
        private int _pendingDamage;

        public string UnitTypeId { get; }
        public int AttackPower { get; }
        public int MaxHealth { get; }
        public int DefensePower { get; }
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;

        /// <summary><paramref name="unitTypeId"/> 종류의 유닛에 최종 공격력 <paramref name="attackPower"/>,
        /// 최대 체력 <paramref name="maxHealth"/>, 방어력 <paramref name="defensePower"/>를 적용한다.</summary>
        protected UnitBase(string unitTypeId, int attackPower, int maxHealth, int defensePower)
        {
            if (string.IsNullOrWhiteSpace(unitTypeId))
            {
                throw new ArgumentException("유닛 종류 ID가 필요합니다.", nameof(unitTypeId));
            }
            if (attackPower < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackPower));
            }
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }
            if (defensePower < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defensePower));
            }

            UnitTypeId = unitTypeId;
            AttackPower = attackPower;
            MaxHealth = maxHealth;
            DefensePower = defensePower;
            CurrentHealth = maxHealth;
        }

        /// <summary>방어 계산을 마친 피해량 <paramref name="resolvedDamage"/>만큼 남은 체력을 줄인다.</summary>
        public void ApplyDamage(int resolvedDamage)
        {
            if (resolvedDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolvedDamage));
            }

            CurrentHealth = Math.Max(0, CurrentHealth - resolvedDamage);
        }

        /// <summary><paramref name="attacker"/>의 이번 틱 공격을 방어력으로 경감한 뒤 합산한다.</summary>
        internal void QueueHitFrom(UnitBase attacker)
        {
            if (attacker.AttackPower > 0)
            {
                int damage = Math.Max(1, attacker.AttackPower - DefensePower);
                _pendingDamage += Math.Min(damage, CurrentHealth - _pendingDamage);
            }
        }

        /// <summary>양쪽 진영이 모두 표적을 정한 뒤 이번 틱에 누적된 피해를 체력에 반영한다.</summary>
        internal void ResolveHits()
        {
            ApplyDamage(_pendingDamage);
            _pendingDamage = 0;
        }
    }
}
