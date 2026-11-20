using System;
using LobbyDefense.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>선택한 보유 카드의 성장 내역과 에너지 잔액을 표시하고 강화와 변환 확인을 처리한다.</summary>
    public sealed class PopupDefenseCard : PopupHandler
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _grade;
        [SerializeField] private DefenseGradeStars _stars;
        [SerializeField] private TMP_Text[] _stats;
        [SerializeField] private Button[] _upgrades;
        [SerializeField] private TMP_Text _energy;
        [SerializeField] private TMP_Text _conversion;
        [SerializeField] private Button _convert;
        [SerializeField] private GameObject _confirmation;
        [SerializeField] private TMP_Text _question;
        [SerializeField] private Button _confirm;
        [SerializeField] private Button _cancel;
        [SerializeField] private Button _close;
        private readonly DefenseCardPortrait _portraitRequest = new DefenseCardPortrait();
        private string _cardId;
        private bool _canConvert;
        internal string UnitTypeId { get; private set; }
        public event Action<string> Converted;
        public event Action<string, DefenseAttribute> Upgraded;

        /// <summary>닫기·확인·취소와 세 능력치 버튼을 연결한다.</summary>
        protected override void Awake()
        {
            base.Awake();
            _popup.OnOpened += FocusVisibleButton;
            _close.onClick.AddListener(ClosePopup);
            _convert.onClick.AddListener(() => SetConfirmation(true));
            _cancel.onClick.AddListener(() => SetConfirmation(false));
            _confirm.onClick.AddListener(Confirm);
            for (int i = 0; i < _upgrades.Length; i++)
            {
                DefenseAttribute stat = (DefenseAttribute)i;
                _upgrades[i].onClick.AddListener(() => Upgrade(stat));
            }
        }

        /// <summary><paramref name="card"/>의 태생·강화·전투 능력치와 <paramref name="game"/>의 종류별 에너지를 표시한다.</summary>
        public void Present(DefenseCampaign game, OwnedDefenseCard card)
        {
            DefenseUnitDefinition definition = game.Rules.Find(card.TypeId);
            if (_cardId != card.Id)
            {
                _cardId = card.Id;
                UnitTypeId = card.TypeId;
                _name.text = definition.Name;
                _grade.text = card.Grade == 4 ? "금색 3성" : $"{card.Grade}성";
                _stars.SetGrade(card.Grade);
            }
            _portraitRequest.Show(_portrait, card.TypeId);
            int energy = game.EnergyFor(card.TypeId);
            bool affordable = energy >= game.Rules.UpgradeCost;
            DefenseUnit unit = card.CreateUnit(definition);
            ShowStat(0, card.Roll.Attack, card.Upgrade.Attack, unit.AttackPower, affordable);
            ShowStat(1, card.Roll.Health, card.Upgrade.Health, unit.MaxHealth, affordable);
            ShowStat(2, card.Roll.Defense, card.Upgrade.Defense, unit.DefensePower, affordable);
            _energy.text = $"{definition.Name} 에너지 {energy}개\n강화 비용 {game.Rules.UpgradeCost}개";
            _canConvert = game.CanConvert(card.Id);
            _convert.interactable = _canConvert;
            _conversion.text = $"카드 1장을 소모해 {definition.Name} 에너지 {game.Rules.ConversionEnergy}개를 획득합니다.";
            if (!_canConvert)
            {
                SetConfirmation(false);
                _conversion.text = game.Session.IsCardDeployed(card.Id) ? "배치 중인 카드는 회수한 뒤 변환할 수 있습니다." : "마지막 공격 카드를 변환하려면 재고용할 골드가 필요합니다.";
            }
            _question.text = $"{definition.Name} 카드 1장을 소모하고\n에너지 {game.Rules.ConversionEnergy}개를 획득할까요?";
        }

        /// <summary><paramref name="index"/>행의 태생 <paramref name="roll"/>, 누적 강화 <paramref name="upgrade"/>, 전투값 <paramref name="total"/>과 비용 충족 여부 <paramref name="affordable"/>를 반영한다.</summary>
        private void ShowStat(int index, int roll, int upgrade, int total, bool affordable)
        {
            int offset = index * 3;
            _stats[offset].SetText("{0}", roll);
            _stats[offset + 1].text = "+" + upgrade;
            _stats[offset + 2].text = total.ToString();
            _upgrades[index].interactable = affordable && total < int.MaxValue;
        }

        /// <summary><paramref name="visible"/>에 따라 변환 확인 창을 열거나 닫는다.</summary>
        private void SetConfirmation(bool visible)
        {
            if (_confirmation.activeSelf == visible)
            {
                return;
            }
            _confirmation.SetActive(visible);
            if (visible)
            {
                _cancel.Select();
            }
            else
            {
                _close.Select();
            }
        }

        /// <summary><paramref name="popup"/>의 열림 연출 중 확인창을 띄운 경우에도 현재 창 안의 버튼을 선택한다.</summary>
        private void FocusVisibleButton(PopupBase popup)
        {
            Button target = _confirmation.activeSelf ? _cancel : _close;
            target.Select();
        }

        /// <summary>확정한 카드 한 장만 소모하도록 고유 ID를 보낸다.</summary>
        private void Confirm()
        {
            if (!_popup.IsAnimating && _canConvert && _confirmation.activeSelf && Converted != null)
            {
                SetConfirmation(false);
                Converted.Invoke(_cardId);
            }
        }

        /// <summary>선택한 카드의 <paramref name="stat"/> 한 가지를 강화하도록 전달한다.</summary>
        private void Upgrade(DefenseAttribute stat)
        {
            if (!_popup.IsAnimating && !_confirmation.activeSelf && Upgraded != null)
            {
                Upgraded.Invoke(_cardId, stat);
            }
        }
    }
}
