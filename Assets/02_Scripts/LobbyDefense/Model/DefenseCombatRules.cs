using System;

namespace LobbyDefense.Model
{
    /// <summary>침공 몬스터 한 종류의 전투 능력치와 한 칸 이동·공격에 걸리는 틱을 정의한다.</summary>
    [Serializable]
    public sealed class DefenseMonsterRules
    {
        public string TypeId;
        public int AttackPower;
        public int MaxHealth;
        public int DefensePower;
        public int MoveTicks;
        public int AttackTicks;

        /// <summary>종류 ID와 능력치, 틱 간격이 침공 몬스터 생성에 유효한지 확인한다.</summary>
        internal bool IsValid() => !string.IsNullOrWhiteSpace(TypeId) && AttackPower >= 0 && MaxHealth > 0
            && DefensePower >= 0 && MoveTicks > 0 && AttackTicks > 0;
    }

    /// <summary>게임 데이터에서 주입한 웨이브와 유령, 마비 트랩의 전투 규칙을 담는다.</summary>
    [Serializable]
    public sealed class DefenseCombatRules
    {
        public DefenseMonsterRules Normal;
        public DefenseMonsterRules Ghost;
        public int NormalSpawnTicks;
        public int GhostSpawnTicks;
        public int WaveSize;
        public int PreparationTicks;
        public int KillGold;
        public int TrapStunTicks;
        public int TrapRechargeTicks;

        /// <summary>세션을 시작하기 전에 몬스터 정의와 소환·트랩 주기, 보상 범위를 검사한다.</summary>
        public void Validate()
        {
            if (Normal == null || !Normal.IsValid() || Ghost == null || !Ghost.IsValid())
            {
                throw new ArgumentException("일반 몬스터와 유령의 종류, 능력치, 이동·공격 주기를 확인하세요.");
            }
            if (NormalSpawnTicks <= 0 || GhostSpawnTicks <= 0 || WaveSize <= 0 || PreparationTicks <= 0
                || KillGold < 0 || (long)WaveSize * KillGold > int.MaxValue || TrapStunTicks <= 0 || TrapRechargeTicks <= 0)
            {
                throw new ArgumentException("웨이브 수, 소환·정비·트랩 주기와 처치 보상 범위를 확인하세요.");
            }
        }
    }
}
