using System;
using LobbyDefense.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>보유 카드 한 장의 초상과 태생 등급을 표시하고 클릭한 카드 ID를 전달한다.</summary>
    public sealed class DefenseInventoryTile : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _portrait;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private DefenseGradeStars _grade;
        [SerializeField] private GameObject _deployed;
        private string _id;
        private string _typeId;
        private Action<string> _select;

        /// <summary>카드를 눌렀을 때 현재 카드 ID를 전달하도록 연결한다.</summary>
        private void Awake() => _button.onClick.AddListener(Select);

        /// <summary>현재 보유 카드 ID를 선택 콜백에 전달한다.</summary>
        private void Select() => _select.Invoke(_id);

        /// <summary><paramref name="card"/>의 이름 <paramref name="unitName"/>과 배치 상태 <paramref name="deployed"/>를 표시하고 선택 시 <paramref name="select"/>를 호출한다.</summary>
        public void Bind(OwnedDefenseCard card, string unitName, bool deployed, Action<string> select)
        {
            _select = select;
            if (_id != card.Id)
            {
                _id = card.Id;
                _name.text = unitName;
                _grade.SetGrade(card.Grade);
            }
            _deployed.SetActive(deployed);
            if (_typeId != card.TypeId)
            {
                _typeId = card.TypeId;
                _portrait.enabled = false;
                string typeId = card.TypeId;
                AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<Sprite>
                {
                    address = "LobbyDefense/Profiles/" + typeId,
                    successCallback = sprite => SetPortrait(typeId, sprite)
                });
            }
        }

        /// <summary>이 칸이 여전히 같은 종류 <paramref name="typeId"/>일 때 도착한 초상 <paramref name="sprite"/>을 표시한다.</summary>
        private void SetPortrait(string typeId, Sprite sprite)
        {
            if (this != null && _typeId == typeId)
            {
                _portrait.sprite = sprite;
                _portrait.enabled = sprite != null;
            }
        }

    }
}
