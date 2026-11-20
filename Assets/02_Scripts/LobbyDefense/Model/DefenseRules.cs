using System;
using System.Collections.Generic;

namespace LobbyDefense.Model
{
    /// <summary>어드레서블 규칙표에 담긴 보드, 전투, 카드 및 고용 설정이다.</summary>
    [Serializable]
    public sealed class DefenseRules
    {
        public const string Address = "LobbyDefense/Rules";
        public int Version;
        public int Width;
        public int Height;
        public int EntranceRow;
        public int ExitRow;
        public int SpawnAreaColumns;
        public int SpawnAreaRowRadius;
        public int HireGold;
        public int ConversionEnergy;
        public int UpgradeCost;
        public int TrapLimit;
        public int[] GradeMaxTotals;
        public string StarterUnitId;
        public DefenseUnitDefinition[] Units;
        public DefenseCombatRules Combat;

        /// <summary>규칙표의 행 번호와 등장 공간 크기로 보드를 만든다. 버전 1은 입구 한 칸을 비운다.</summary>
        public DefenseBoard CreateBoard()
        {
            int entrance = (EntranceRow - 1) * Width;
            int exit = ExitRow * Width - 1;
            if (Version == 1)
            {
                return new DefenseBoard(Width, Height, entrance, exit);
            }
            return new DefenseBoard(Width, Height, entrance, exit, SpawnAreaColumns, SpawnAreaRowRadius);
        }

        /// <summary>발급 시점의 태생 점수 <paramref name="roll"/>로 등급을 결정한다. 금색 3성의 저장값은 4다.</summary>
        internal int GradeFor(DefenseStats roll)
        {
            int total = roll.Attack + roll.Health + roll.Defense;
            for (int i = 0; i < GradeMaxTotals.Length; i++)
            {
                if (total <= GradeMaxTotals[i])
                {
                    return i + 1;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(roll));
        }

        /// <summary><paramref name="id"/>와 일치하는 유닛 정의를 찾는다.</summary>
        public DefenseUnitDefinition Find(string id)
        {
            foreach (DefenseUnitDefinition unit in Units)
            {
                if (unit.Id == id)
                {
                    return unit;
                }
            }
            return null;
        }

        /// <summary>규칙표를 사용하기 전에 보드 크기, 비용, 카드 종류와 시작 카드를 확인한다.</summary>
        public void Validate()
        {
            if (Version < 1 || Width < 2 || Height < 1 || (long)Width * Height > int.MaxValue ||
                EntranceRow < 1 || EntranceRow > Height || ExitRow < 1 || ExitRow > Height ||
                HireGold < 1 || UpgradeCost < 1 || ConversionEnergy < 1 || TrapLimit < 0 || Combat == null ||
                Units == null || Units.Length == 0 || GradeMaxTotals == null || GradeMaxTotals.Length != 4 ||
                GradeMaxTotals[0] < 0 || GradeMaxTotals[0] >= GradeMaxTotals[1] || GradeMaxTotals[1] >= 29 ||
                GradeMaxTotals[2] != 29 || GradeMaxTotals[3] != 30)
            {
                throw new ArgumentException("로비 디펜스 규칙표의 보드 또는 경제 설정이 잘못되었습니다.");
            }
            if (Version >= 2 && (SpawnAreaColumns < 1 || SpawnAreaColumns > Width || SpawnAreaRowRadius < 0 || SpawnAreaRowRadius >= Height))
            {
                throw new ArgumentException("몬스터 등장 공간의 열 수와 위아래 행 범위를 확인해 주세요.");
            }
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (DefenseUnitDefinition unit in Units)
            {
                if (unit == null || !unit.IsValid() || !ids.Add(unit.Id))
                {
                    throw new ArgumentException("유닛 종류 또는 전투 설정을 확인해 주세요.");
                }
            }
            if (!ids.Contains(StarterUnitId) || Find(StarterUnitId).Range == 0 || Find(StarterUnitId).Base.Attack <= 0)
            {
                throw new ArgumentException("시작 카드의 유닛 종류가 없습니다.");
            }
            Combat.Validate();
        }
    }

    /// <summary>한 카드 종류의 표시 이름, 전투 기본값, 태생 점수당 보정치와 공격 주기를 보관한다.</summary>
    [Serializable]
    public sealed class DefenseUnitDefinition
    {
        public string Id;
        public string Name;
        public int Range;
        public int AttackTicks;
        public DefenseStats Base;
        public DefenseStats PerPoint;

        /// <summary>기본 능력치, 최대 태생 점수에서의 값과 공격 설정을 검사한다.</summary>
        internal bool IsValid() => !string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(Name) &&
            Base.IsValid && Base.Health > 0 && PerPoint.IsValid && Range >= 0 && AttackTicks > 0 &&
            (long)Base.Attack + PerPoint.Attack * 10L < int.MaxValue &&
            (long)Base.Health + PerPoint.Health * 10L < int.MaxValue &&
            (long)Base.Defense + PerPoint.Defense * 10L < int.MaxValue;
    }

    /// <summary>공격력, 체력, 방어력을 같은 순서로 묶는다.</summary>
    [Serializable]
    public struct DefenseStats
    {
        public int Attack;
        public int Health;
        public int Defense;
        internal bool IsValid => Attack >= 0 && Health >= 0 && Defense >= 0;
    }
}
