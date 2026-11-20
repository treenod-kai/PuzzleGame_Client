using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>인벤토리에 선택한 카드의 초상·능력치와 같은 종류의 에너지를 표시하고 강화·변환 입력을 전달한다.</summary>
    public sealed class DefenseCardInventoryView : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(66, 46, 39, 255);
        private static readonly Color Teal = new Color32(31, 100, 99, 255);
        private static readonly Color Copper = new Color32(148, 72, 36, 255);

        public event Action<int> CardChanged;
        public event Action Converted;
        public event Action<int> Upgraded;

        private TMP_FontAsset _font;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _details;
        private Image _portrait;
        private Button _previous;
        private Button _next;
        private readonly Button[] _upgrades = new Button[3];
        private Button _convert;
        private RectTransform _actions;
        private RectTransform _confirmation;
        private string _shownType;

        /// <summary>기존 로비 글꼴 <paramref name="font"/>를 사용해 비활성 인벤토리에도 카드 화면을 준비한다.</summary>
        public void Initialize(TMP_FontAsset font)
        {
            if (_title != null)
            {
                return;
            }

            _font = font;
            Transform placeholderTitle = transform.Find("Title");
            if (_font == null && placeholderTitle != null)
            {
                TextMeshProUGUI titleText = placeholderTitle.GetComponent<TextMeshProUGUI>();
                if (titleText != null)
                {
                    _font = titleText.font;
                }
            }
            HidePlaceholder("Status");
            HidePlaceholder("Illustration");

            _portrait = MakeRect(transform, "초상", 0, 156, 144, 144).gameObject.AddComponent<Image>();
            _portrait.preserveAspect = true;
            _portrait.raycastTarget = false;
            _previous = MakeButton(transform, "이전", -182, 155, 110, Teal, () => Browse(-1));
            _next = MakeButton(transform, "다음", 182, 155, 110, Teal, () => Browse(1));
            _title = MakeText(transform, "카드 이름", 0, 60, 510, 52, 31);
            _title.fontStyle = FontStyles.Bold;
            _details = MakeText(transform, "능력치", 0, -42, 510, 146, 24);

            _actions = MakeRect(transform, "카드 관리", 0, -198, 540, 140);
            string[] labels = { "공격 강화", "체력 강화", "방어 강화" };
            for (int i = 0; i < labels.Length; i++)
            {
                int stat = i;
                _upgrades[i] = MakeButton(_actions, labels[i], (i - 1) * 172, 35, 159, Teal, () => Upgrade(stat));
            }
            _convert = MakeButton(_actions, "에너지로 변환", 0, -43, 326, Copper, AskConversion);

            _confirmation = MakeRect(transform, "변환 확인", 0, -198, 540, 140);
            MakeText(_confirmation, "이 카드 한 장을 에너지로 변환할까요?", 0, 35, 526, 58, 25);
            MakeButton(_confirmation, "취소", -112, -43, 205, Teal, CancelConversion);
            MakeButton(_confirmation, "변환 확정", 112, -43, 205, Copper, ConfirmConversion);
            ShowCard(null, null, null, false, false, false);
        }

        /// <summary>종류 <paramref name="unitTypeId"/>의 이름·등급 <paramref name="title"/>과 능력치·에너지 <paramref name="details"/>를 표시하고
        /// 변환 <paramref name="canConvert"/>, 강화 <paramref name="canUpgrade"/>, 탐색 <paramref name="canBrowse"/> 가능 여부를 반영한다.</summary>
        public void ShowCard(string unitTypeId, string title, string details, bool canConvert, bool canUpgrade, bool canBrowse)
        {
            bool hasCard = !string.IsNullOrEmpty(unitTypeId);
            bool keepConfirmation = _confirmation.gameObject.activeSelf && hasCard && canConvert &&
                _shownType == unitTypeId && _title.text == title && _details.text == details;
            if (!keepConfirmation)
            {
                CancelConversion();
            }
            _actions.gameObject.SetActive(hasCard && !keepConfirmation);
            _previous.gameObject.SetActive(hasCard && canBrowse);
            _next.gameObject.SetActive(hasCard && canBrowse);
            _title.text = !hasCard && string.IsNullOrEmpty(title) ? "보유한 카드가 없습니다" : title;
            _details.text = details;
            _convert.interactable = hasCard && canConvert;
            for (int i = 0; i < _upgrades.Length; i++)
            {
                _upgrades[i].interactable = hasCard && canUpgrade;
            }

            if (!hasCard || unitTypeId != _shownType)
            {
                _shownType = unitTypeId;
                _portrait.enabled = false;
                _portrait.sprite = null;
                if (hasCard)
                {
                    AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<Sprite>
                    {
                        address = "LobbyDefense/Profiles/" + unitTypeId,
                        successCallback = sprite => SetPortrait(unitTypeId, sprite)
                    });
                }
            }
        }

        /// <summary>비동기로 도착한 <paramref name="sprite"/>를 현재 선택한 종류 <paramref name="unitTypeId"/>에만 반영한다.</summary>
        private void SetPortrait(string unitTypeId, Sprite sprite)
        {
            if (this != null && unitTypeId == _shownType)
            {
                _portrait.sprite = sprite;
                _portrait.enabled = sprite != null;
            }
        }

        /// <summary>패널을 닫으면 진행 중이던 변환 확인도 닫는다.</summary>
        private void OnDisable()
        {
            if (_confirmation != null && _confirmation.gameObject.activeSelf)
            {
                CancelConversion();
            }
        }

        /// <summary>카드 목록에서 <paramref name="direction"/>만큼 앞이나 뒤로 이동한다.</summary>
        private void Browse(int direction)
        {
            if (CardChanged != null)
            {
                CardChanged(direction);
            }
        }

        /// <summary>선택 카드에서 <paramref name="stat"/>에 해당하는 능력치 하나의 강화를 요청한다.</summary>
        private void Upgrade(int stat)
        {
            if (Upgraded != null)
            {
                Upgraded(stat);
            }
        }

        /// <summary>조회 중인 카드의 변환을 확인할 동안 강화와 카드 이동을 잠근다.</summary>
        private void AskConversion()
        {
            _actions.gameObject.SetActive(false);
            _confirmation.gameObject.SetActive(true);
            _previous.interactable = false;
            _next.interactable = false;
        }

        /// <summary>확인을 닫고 현재 카드의 조작 버튼을 복원한다.</summary>
        private void CancelConversion()
        {
            _confirmation.gameObject.SetActive(false);
            _actions.gameObject.SetActive(true);
            _previous.interactable = true;
            _next.interactable = true;
        }

        /// <summary>확인 화면을 닫은 후 선택 카드 한 장의 변환을 요청한다.</summary>
        private void ConfirmConversion()
        {
            CancelConversion();
            if (Converted != null)
            {
                Converted();
            }
        }

        /// <summary>인벤토리 프리팹의 임시 콘텐츠 <paramref name="childName"/>를 숨긴다.</summary>
        private void HidePlaceholder(string childName)
        {
            Transform child = transform.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        /// <summary>부모 <paramref name="parent"/>에 이름 <paramref name="name"/>인 영역을 중앙 기준 <paramref name="x"/>, <paramref name="y"/>에
        /// 가로 <paramref name="width"/>, 세로 <paramref name="height"/>로 만든다.</summary>
        private static RectTransform MakeRect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject element = new GameObject(name, typeof(RectTransform));
            element.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)element.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>부모 <paramref name="parent"/>의 <paramref name="x"/>, <paramref name="y"/> 위치에 가로 <paramref name="width"/>, 세로
        /// <paramref name="height"/>인 글 상자를 만들어 <paramref name="label"/>을 최대 크기 <paramref name="size"/>로 표시한다.</summary>
        private TextMeshProUGUI MakeText(Transform parent, string label, float x, float y, float width, float height, float size)
        {
            TextMeshProUGUI text = MakeRect(parent, label, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                text.font = _font;
            }
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = 17f;
            text.color = Ink;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>부모 <paramref name="parent"/>의 <paramref name="x"/>, <paramref name="y"/>에 이름 <paramref name="label"/>, 너비
        /// <paramref name="width"/>, 색 <paramref name="color"/>인 버튼을 만들고 클릭 동작 <paramref name="onClick"/>을 연결한다.</summary>
        private Button MakeButton(Transform parent, string label, float x, float y, float width, Color color, UnityAction onClick)
        {
            RectTransform rect = MakeRect(parent, label, x, y, width, 63);
            Image background = rect.gameObject.AddComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
            colors.selectedColor = color;
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.disabledColor = new Color32(122, 118, 108, 255);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            MakeText(rect, label, 0, 0, width - 12, 53, 24).color = Color.white;
            return button;
        }
    }
}
