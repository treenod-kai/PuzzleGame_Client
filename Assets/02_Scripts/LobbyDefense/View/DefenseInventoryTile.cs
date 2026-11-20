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
        private readonly DefenseCardPortrait _portraitRequest = new DefenseCardPortrait();
        private Action<string> _select;

        /// <summary>카드를 눌렀을 때 현재 카드 ID를 전달하도록 연결한다.</summary>
        private void Awake() => _button.onClick.AddListener(Select);

        /// <summary>목록을 다시 열면 이전에 실패한 초상을 바로 요청할 수 있게 한다.</summary>
        private void OnDisable() => _portraitRequest.AllowRetry();

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
            _portraitRequest.Show(_portrait, card.TypeId);
        }
    }

    /// <summary>카드 초상을 불러오고 실패하면 재시도를 허용한다. 이전 바인딩의 응답은 무시한다.</summary>
    internal sealed class DefenseCardPortrait
    {
        private const double RetryDelaySeconds = 2d;
        private string _typeId;
        private int _requestVersion;
        private bool _pending;
        private double _retryAt;

        /// <summary>다음 표시 때 실패한 요청의 대기를 해제한다.</summary>
        internal void AllowRetry() => _retryAt = 0d;

        /// <summary><paramref name="target"/>에 <paramref name="typeId"/>의 초상을 표시하고 실패한 요청은 잠시 뒤 다시 허용한다.</summary>
        internal void Show(Image target, string typeId)
        {
            if (_typeId == typeId && (_pending || target.enabled || Time.unscaledTimeAsDouble < _retryAt))
            {
                return;
            }
            _typeId = typeId;
            _pending = true;
            _retryAt = 0d;
            target.enabled = false;
            Request(target, typeId, ++_requestVersion);
        }

        /// <summary><paramref name="target"/>에 쓸 <paramref name="typeId"/>의 초상을 요청하고 응답을 <paramref name="version"/>에 연결한다.</summary>
        private void Request(Image target, string typeId, int version)
        {
            AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<Sprite>
            {
                address = "LobbyDefense/Profiles/" + typeId,
                successCallback = sprite => Complete(target, version, sprite),
                failedCallback = () => Complete(target, version, null)
            });
        }

        /// <summary><paramref name="target"/>의 현재 요청 <paramref name="version"/>에만 <paramref name="sprite"/>를 적용하고 실패하면 재시도 시점을 정한다.</summary>
        private void Complete(Image target, int version, Sprite sprite)
        {
            if (target == null || _requestVersion != version)
            {
                return;
            }
            _pending = false;
            target.sprite = sprite;
            target.enabled = sprite != null;
            if (sprite == null && target.gameObject.activeInHierarchy)
            {
                _retryAt = Time.unscaledTimeAsDouble + RetryDelaySeconds;
            }
        }
    }
}
