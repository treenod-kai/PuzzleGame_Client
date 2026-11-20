using System;
using System.Collections.Generic;
using System.IO;
using Puzzle.Core;

namespace LobbyDefense.Model
{
    /// <summary>한 유저의 카드, 재화와 로비 전투를 함께 진행하고 같은 시점으로 저장한다.</summary>
    public sealed class DefenseCampaign
    {
        private readonly DefenseProgress _progress;
        private readonly PuzzleRandom _random;
        public DefenseRules Rules { get; }
        public DefenseSession Session { get; }
        public IReadOnlyList<OwnedDefenseCard> Cards => _progress.Cards;
        public int Gold => _progress.Gold;
        public int Revision { get; private set; }

        /// <summary><paramref name="rules"/>와 추첨기 <paramref name="random"/>으로 <paramref name="saved"/>를 열거나 처음 접속한 유저에게 카드를 지급한다.</summary>
        public DefenseCampaign(DefenseRules rules, PuzzleRandom random, DefenseProgress saved)
        {
            Rules = rules;
            _random = random;
            if (saved == null)
            {
                _progress = new DefenseProgress { Format = 1, RuleVersion = rules.Version };
                Session = new DefenseSession(rules.CreateBoard(), rules.Combat);
                _progress.Cards.Add(Draw(rules.StarterUnitId));
                return;
            }
            bool resizeBoard = saved.RuleVersion == 1 && rules.Version == 2;
            if (saved.Format != 1 || (saved.RuleVersion != rules.Version && !resizeBoard) || saved.Gold < 0 ||
                saved.Cards == null || saved.Energy == null || saved.Battle == null)
            {
                throw new InvalidDataException("디펜스 저장 형식 또는 규칙 버전을 확인해 주세요.");
            }
            _progress = saved;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (OwnedDefenseCard card in saved.Cards)
            {
                if (card == null || !card.IsValid(rules) || !ids.Add(card.Id))
                {
                    throw new InvalidDataException("보유 카드 데이터가 올바르지 않습니다.");
                }
                DefenseUnit unit = card.CreateUnit(rules.Find(card.TypeId));
                if (card.ReserveHealth > unit.MaxHealth)
                {
                    throw new InvalidDataException("회수한 카드의 남은 체력이 최대 체력을 초과합니다.");
                }
            }
            ids.Clear();
            foreach (DefenseEnergy energy in saved.Energy)
            {
                if (energy == null || rules.Find(energy.TypeId) == null || energy.Amount < 0 || !ids.Add(energy.TypeId))
                {
                    throw new InvalidDataException("유닛 에너지 데이터가 올바르지 않습니다.");
                }
            }
            DefenseBoard board = rules.CreateBoard();
            // 규칙 1의 입구는 (1, 5), 출구는 (100, 5)였다. 잘려 나갈 칸도 먼저 당시 규칙으로 검사한다.
            DefenseBoard savedBoard = resizeBoard ? new DefenseBoard(100, 10, 400, 499) : board;
            DefenseSession restored = DefenseSession.Restore(savedBoard, rules.Combat, saved.Battle);
            if (restored.Traps.Count > rules.TrapLimit)
            {
                throw new InvalidDataException("저장된 트랩 수가 설치 한도를 초과합니다.");
            }
            foreach (DeployedDefender defender in restored.Defenders)
            {
                OwnedDefenseCard card = FindCard(defender.Unit.CardInstanceId);
                if (card == null || card.TypeId != defender.Unit.UnitTypeId)
                {
                    throw new InvalidDataException("배치된 유닛과 보유 카드가 일치하지 않습니다.");
                }
                if (resizeBoard && defender.Unit.CurrentHealth > card.CreateUnit(rules.Find(card.TypeId)).MaxHealth)
                {
                    throw new InvalidDataException("배치된 카드의 남은 체력이 최대 체력을 초과합니다.");
                }
            }
            if (!resizeBoard)
            {
                Session = restored;
                return;
            }
            DefenseSessionState resized = restored.CaptureForBoard(board);
            Session = DefenseSession.Restore(board, rules.Combat, resized);
            foreach (DeployedDefender defender in restored.Defenders)
            {
                if (!Session.IsCardDeployed(defender.Unit.CardInstanceId))
                {
                    FindCard(defender.Unit.CardInstanceId).ReserveHealth = defender.Unit.CurrentHealth;
                }
            }
            saved.Battle = resized;
            saved.RuleVersion = rules.Version;
        }

        /// <summary>카드, 골드와 전투를 같은 틱에서 직렬화하도록 현재 세션을 저장 데이터에 담는다.</summary>
        public DefenseProgress Capture()
        {
            _progress.Battle = Session.Capture();
            return _progress;
        }

        /// <summary><paramref name="id"/>와 일치하는 보유 카드 한 장을 찾는다.</summary>
        public OwnedDefenseCard FindCard(string id) => _progress.Cards.Find(card => card.Id == id);

        /// <summary><paramref name="typeId"/> 종류의 카드들이 공유하는 에너지 잔액을 반환한다.</summary>
        public int EnergyFor(string typeId)
        {
            DefenseEnergy energy = _progress.Energy.Find(entry => entry.TypeId == typeId);
            return energy == null ? 0 : energy.Amount;
        }

        /// <summary><paramref name="typeId"/> 카드 한 장의 세 능력치를 각각 균등 추첨하고 고유 등급을 확정한다.</summary>
        private OwnedDefenseCard Draw(string typeId)
        {
            DefenseStats roll = new DefenseStats { Attack = _random.Next(0, 11), Health = _random.Next(0, 11), Defense = _random.Next(0, 11) };
            return new OwnedDefenseCard { Id = Guid.NewGuid().ToString("N"), TypeId = typeId, Roll = roll, Grade = Rules.GradeFor(roll) };
        }

        /// <summary>파괴된 <paramref name="card"/>를 다음 정비부터 다시 배치할 수 있는지 확인한다.</summary>
        public bool IsRecovered(OwnedDefenseCard card) => card.ReadyWave <= (long)Session.Wave + (Session.IsPreparing ? 1 : 0);

        /// <summary><paramref name="id"/>의 카드를 <paramref name="cell"/>에 배치하거나 정비 중 같은 카드를 이동한다.</summary>
        public bool TryPlace(string id, int cell)
        {
            OwnedDefenseCard card = FindCard(id);
            if (card == null || !IsRecovered(card) || !Session.CanPlace(cell))
            {
                return false;
            }
            if (Session.IsCardDeployed(id))
            {
                return Session.TryMove(id, cell);
            }
            return Place(card, cell);
        }

        /// <summary>카드 <paramref name="card"/>의 현재 능력치를 계산하여 <paramref name="cell"/>에 배치한다.</summary>
        private bool Place(OwnedDefenseCard card, int cell)
        {
            DefenseUnitDefinition definition = Rules.Find(card.TypeId);
            DefenseUnit unit = card.CreateUnit(definition);
            if (card.ReserveHealth > 0)
            {
                unit.ApplyDamage(Math.Max(0, unit.MaxHealth - card.ReserveHealth));
            }
            if (!Session.TryPlace(unit, cell, definition.Range, definition.AttackTicks))
            {
                return false;
            }
            card.ReserveHealth = 0;
            Revision++;
            return true;
        }

        /// <summary><paramref name="typeId"/> 고용 비용과 이후 적을 처치할 수단이 남는지 확인한다.</summary>
        public bool CanHire(string typeId)
        {
            DefenseUnitDefinition definition = Rules.Find(typeId);
            return definition != null && Gold >= Rules.HireGold &&
                (definition.Range > 0 || Gold - Rules.HireGold >= Rules.HireGold || HasAttackerExcept(null));
        }

        /// <summary><paramref name="excluded"/>를 제외하고 적을 사격할 수 있는 카드를 보유하는지 확인한다.</summary>
        private bool HasAttackerExcept(string excluded)
        {
            foreach (OwnedDefenseCard card in _progress.Cards)
            {
                if (card.Id != excluded && Rules.Find(card.TypeId).Range > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>골드로 <paramref name="typeId"/> 카드 한 장을 뽑고 <paramref name="cell"/> 배치에 성공하면 비용을 차감한다.</summary>
        public bool TryHire(string typeId, int cell)
        {
            if (!CanHire(typeId) || !Session.CanPlace(cell))
            {
                return false;
            }
            OwnedDefenseCard card = Draw(typeId);
            if (!Place(card, cell))
            {
                return false;
            }
            _progress.Cards.Add(card);
            _progress.Gold -= Rules.HireGold;
            return true;
        }

        /// <summary>정비 중 <paramref name="id"/> 카드를 회수하고 남은 체력을 보관한다.</summary>
        public bool TryRecall(string id)
        {
            foreach (DeployedDefender deployed in Session.Defenders)
            {
                if (deployed.Unit.CardInstanceId == id)
                {
                    int health = deployed.Unit.CurrentHealth;
                    if (!Session.TryRemove(id))
                    {
                        return false;
                    }
                    FindCard(id).ReserveHealth = health;
                    Revision++;
                    return true;
                }
            }
            return false;
        }

        /// <summary><paramref name="id"/>의 카드를 변환한 뒤에도 적을 처치하거나 새 사격 카드를 고용할 수 있는지 확인한다.</summary>
        public bool CanConvert(string id) => FindCard(id) != null && !Session.IsCardDeployed(id) &&
            (Gold >= Rules.HireGold || HasAttackerExcept(id));

        /// <summary><paramref name="id"/>의 미배치 카드를 한 장 소모해 그 종류의 에너지를 지급한다.</summary>
        public bool TryConvert(string id)
        {
            OwnedDefenseCard card = FindCard(id);
            if (!CanConvert(id))
            {
                return false;
            }
            DefenseEnergy energy = _progress.Energy.Find(entry => entry.TypeId == card.TypeId);
            if (energy == null)
            {
                energy = new DefenseEnergy { TypeId = card.TypeId };
                _progress.Energy.Add(energy);
            }
            if (energy.Amount > int.MaxValue - Rules.ConversionEnergy)
            {
                return false;
            }
            energy.Amount += Rules.ConversionEnergy;
            _progress.Cards.Remove(card);
            Revision++;
            return true;
        }

        /// <summary>공용 에너지를 차감해 <paramref name="id"/> 카드의 <paramref name="stat"/> 능력치 하나를 강화한다.</summary>
        public bool TryUpgrade(string id, DefenseAttribute stat)
        {
            OwnedDefenseCard card = FindCard(id);
            if (card == null)
            {
                return false;
            }
            DefenseEnergy energy = _progress.Energy.Find(entry => entry.TypeId == card.TypeId);
            if (energy == null || energy.Amount < Rules.UpgradeCost || !card.TryUpgrade(Rules.Find(card.TypeId), stat))
            {
                return false;
            }
            energy.Amount -= Rules.UpgradeCost;
            if (stat == DefenseAttribute.Health && card.ReserveHealth > 0)
            {
                card.ReserveHealth++;
            }
            Session.UpdateCardStats(card.CreateUnit(Rules.Find(card.TypeId)));
            Revision++;
            return true;
        }

        /// <summary>세션을 한 틱 진행해 처치 골드와 파괴된 카드의 복귀 가능 웨이브를 기록한다.</summary>
        public void Step()
        {
            int recoveryWave = Session.Wave + (Session.IsPreparing ? 2 : 1);
            int reward = Session.Step();
            _progress.Gold = (int)Math.Min(int.MaxValue, (long)Gold + reward);
            for (int i = 0; i < Session.DestroyedCards.Count; i++)
            {
                FindCard(Session.DestroyedCards[i]).ReadyWave = recoveryWave;
            }
            if (reward > 0 || Session.DestroyedCards.Count > 0)
            {
                Revision++;
            }
        }
    }

    /// <summary>계정별로 한 번에 저장할 로비의 카드, 재화와 전투 상태다.</summary>
    [Serializable]
    public sealed class DefenseProgress
    {
        public int Format;
        public int RuleVersion;
        public int Gold;
        public List<OwnedDefenseCard> Cards = new List<OwnedDefenseCard>();
        public List<DefenseEnergy> Energy = new List<DefenseEnergy>();
        public DefenseSessionState Battle;
    }

    /// <summary>같은 종류의 보유 카드들이 함께 쓰는 에너지다.</summary>
    [Serializable]
    public sealed class DefenseEnergy
    {
        public string TypeId;
        public int Amount;
    }
}
