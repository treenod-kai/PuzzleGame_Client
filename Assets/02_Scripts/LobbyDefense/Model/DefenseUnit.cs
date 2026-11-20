using System;

namespace LobbyDefense.Model
{
    /// <summary>유저가 보유 카드 한 장을 방어선에 배치한 유닛이다. 공격형과 길막형 모두 이 진영에 속한다.</summary>
    public class DefenseUnit : UnitBase
    {
        public string CardInstanceId { get; }

        /// <summary>보유 카드 <paramref name="cardInstanceId"/>를 <paramref name="unitTypeId"/> 종류로 배치하고
        /// 카드의 최종 공격력 <paramref name="attackPower"/>, 최대 체력 <paramref name="maxHealth"/>, 방어력 <paramref name="defensePower"/>를 사용한다.</summary>
        public DefenseUnit(string cardInstanceId, string unitTypeId, int attackPower, int maxHealth, int defensePower)
            : base(unitTypeId, attackPower, maxHealth, defensePower)
        {
            if (string.IsNullOrWhiteSpace(cardInstanceId))
            {
                throw new ArgumentException("배치할 보유 카드 ID가 필요합니다.", nameof(cardInstanceId));
            }

            CardInstanceId = cardInstanceId;
        }
    }
}
