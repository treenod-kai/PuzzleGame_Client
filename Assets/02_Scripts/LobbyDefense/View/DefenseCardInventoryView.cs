using System;
using System.Collections.Generic;
using System.Text;
using LobbyDefense.Model;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyDefense.View
{
    /// <summary>현재 계정의 방어 카드를 스크롤 목록에 표시하고 선택한 카드의 상세 창을 연결한다.</summary>
    public sealed class DefenseCardInventoryView : MonoBehaviour, IEndDragHandler
    {
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private RectTransform _grid;
        [SerializeField] private DefenseInventoryTile _template;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private TMP_Text _state;
        [SerializeField] private TMP_Text _balances;
        private readonly List<DefenseInventoryTile> _tiles = new List<DefenseInventoryTile>();
        private GridLayoutGroup _layout;
        private VerticalLayoutGroup _contentLayout;
        private Action<string> _openCard;
        private int _firstTile = -1;
        private int _inventoryRevision = -1;
        private int _boardRevision = -1;
        private bool _canRehire;
        private DefenseCampaign _game;
        private string _selectedId;
        private string _receipt;
        private GameObject _previousSelection;
        private PopupBase _popup;
        private PopupDefenseCard _detail;
        private CanvasGroup _lockedControls;
        public event Action<string> Converted;
        public event Action<string, DefenseAttribute> Upgraded;

        /// <summary>스크롤 위치에 맞춰 보이는 행의 카드만 재사용하도록 연결한다.</summary>
        private void Awake()
        {
            _layout = _grid.GetComponent<GridLayoutGroup>();
            _contentLayout = _scroll.content.GetComponent<VerticalLayoutGroup>();
            _openCard = OpenCard;
            _scroll.onValueChanged.AddListener(OnScroll);
        }

        /// <summary>스크롤 위치 <paramref name="position"/>이 바뀌면 화면 앞뒤 한 행까지 바인딩한다.</summary>
        private void OnScroll(Vector2 position)
        {
            if (_game != null && isActiveAndEnabled)
            {
                RenderTiles(false);
            }
        }

        /// <summary><paramref name="game"/>의 보유 목록과 에너지를 갱신하고 열려 있는 상세 창의 카드 ID를 다시 확인한다.</summary>
        public void ShowInventory(DefenseCampaign game)
        {
            if (_game != game)
            {
                ShowState("");
                _game = game;
            }
            if (isActiveAndEnabled)
            {
                Render();
            }
        }

        /// <summary>탭을 열 때 현재 계정의 목록을 표시한다.</summary>
        private void OnEnable()
        {
            _inventoryRevision = -1;
            if (_game != null)
            {
                Render();
            }
        }

        /// <summary>스크롤 위치를 유지하면서 카드 목록·잔액과 열린 상세 창을 현재 상태로 맞춘다.</summary>
        private void Render()
        {
            DefenseCampaign game = _game;
            bool inventoryChanged = _inventoryRevision != game.InventoryRevision;
            bool boardChanged = _boardRevision != game.Session.Board.Revision;
            bool canRehire = game.Gold >= game.Rules.HireGold;
            if (!inventoryChanged && !boardChanged && canRehire == _canRehire)
            {
                return;
            }
            _inventoryRevision = game.InventoryRevision;
            _boardRevision = game.Session.Board.Revision;
            _canRehire = canRehire;
            if (inventoryChanged || boardChanged)
            {
                RenderTiles(true);
            }
            int count = game.Cards.Count;
            if (inventoryChanged)
            {
                _count.text = $"보유 {count}장";
                ShowBalances(game);
            }
            if (_detail != null)
            {
                OwnedDefenseCard selected = game.FindCard(_selectedId);
                if (selected != null)
                {
                    _detail.Present(game, selected);
                }
                else
                {
                    string type = _detail.UnitTypeId;
                    _receipt = $"{game.Rules.Find(type).Name} 에너지 +{game.Rules.ConversionEnergy}   /   보유 {game.EnergyFor(type)}개";
                    Dismiss();
                }
            }
            _state.gameObject.SetActive(count == 0 || !string.IsNullOrEmpty(_receipt));
            _state.text = _receipt;
            if (count == 0)
            {
                _state.text = string.IsNullOrEmpty(_receipt) ? "보유 카드가 없습니다. 로비에서 유닛을 고용하세요." : _receipt + "   /   보유 카드 없음";
            }
        }

        /// <summary>화면에 필요한 행만 만들고 <paramref name="force"/>가 참이면 같은 위치의 카드 정보도 갱신한다.</summary>
        private void RenderTiles(bool force)
        {
            float rowHeight = _layout.cellSize.y + _layout.spacing.y;
            float width = _scroll.viewport.rect.width - _contentLayout.padding.horizontal - _layout.padding.horizontal;
            int columns = Mathf.Max(1, Mathf.FloorToInt((width + _layout.spacing.x + 0.001f) / (_layout.cellSize.x + _layout.spacing.x)));
            int count = _game.Cards.Count;
            int rows = Mathf.CeilToInt((float)count / columns);
            int visibleRows = Mathf.Min(rows, Mathf.CeilToInt(_scroll.viewport.rect.height / rowHeight) + 3);
            float offset = _scroll.content.anchoredPosition.y - _contentLayout.padding.top;
            int firstRow = Mathf.Clamp(Mathf.FloorToInt(offset / rowHeight) - 1, 0, rows - visibleRows);
            int first = firstRow * columns;
            int visible = Mathf.Min(count - first, visibleRows * columns);
            // 위아래의 생략된 행은 여백으로 남겨 스크롤 범위와 마지막 에너지 표시 위치를 유지한다.
            int top = Mathf.RoundToInt(firstRow * rowHeight);
            int bottom = Mathf.RoundToInt((rows - firstRow - visibleRows) * rowHeight);
            if (_layout.padding.top != top || _layout.padding.bottom != bottom)
            {
                _layout.padding.top = top;
                _layout.padding.bottom = bottom;
                LayoutRebuilder.MarkLayoutForRebuild(_grid);
            }
            if (!force && _firstTile == first && _tiles.Count == visible)
            {
                return;
            }
            _firstTile = first;
            TrimTiles(visible);
            for (int i = 0; i < visible; i++)
            {
                OwnedDefenseCard card = _game.Cards[first + i];
                if (i == _tiles.Count)
                {
                    _tiles.Add(Instantiate(_template, _grid));
                }
                DefenseInventoryTile tile = _tiles[i];
                tile.Bind(card, _game.Rules.Find(card.TypeId).Name, _game.Session.IsCardDeployed(card.Id), _openCard);
                if (!tile.gameObject.activeSelf)
                {
                    tile.gameObject.SetActive(true);
                }
            }
        }

        /// <summary><paramref name="game"/>의 에너지 잔액을 종류별로 표시한다.</summary>
        private void ShowBalances(DefenseCampaign game)
        {
            StringBuilder balances = new StringBuilder();
            foreach (DefenseUnitDefinition unit in game.Rules.Units)
            {
                int amount = game.EnergyFor(unit.Id);
                if (amount > 0)
                {
                    balances.AppendLine($"{unit.Name}   {amount}개");
                }
            }
            _balances.gameObject.SetActive(balances.Length > 0);
            _balances.text = "보유 에너지\n" + balances.ToString().TrimEnd();
        }

        /// <summary>이전 계정의 카드와 창을 정리하고 로드·실패 상태 <paramref name="message"/>를 표시한다.</summary>
        public void ShowState(string message)
        {
            Dismiss();
            _game = null;
            _inventoryRevision = -1;
            _firstTile = -1;
            _receipt = null;
            TrimTiles(0);
            _scroll.StopMovement();
            _scroll.content.anchoredPosition = Vector2.zero;
            _state.text = message;
            _state.gameObject.SetActive(true);
            _count.text = "";
            _balances.gameObject.SetActive(false);
        }

        /// <summary>남길 카드 <paramref name="count"/> 이후의 칸을 즉시 숨기고 정리한다.</summary>
        private void TrimTiles(int count)
        {
            for (int i = _tiles.Count - 1; i >= count; i--)
            {
                GameObject tile = _tiles[i].gameObject;
                tile.SetActive(false);
                Destroy(tile);
                _tiles.RemoveAt(i);
            }
        }

        /// <summary>목록에서 누른 <paramref name="id"/>를 고정하고 팝업이 닫힐 때까지 뒤쪽 조작을 잠근다.</summary>
        private void OpenCard(string id)
        {
            if (_lockedControls != null || _game == null || _game.FindCard(id) == null)
            {
                return;
            }
            LobbyNavigation navigation = GetComponentInParent<LobbyNavigation>();
            if (!navigation.CanOpenTab || !ReferenceEquals(DomainManager.Instance.Peek(), GetComponentInParent<LobbyOverlayTab>()))
            {
                return;
            }
            _selectedId = id;
            _receipt = null;
            _state.gameObject.SetActive(false);
            EventSystem events = EventSystem.current;
            if (events != null)
            {
                _previousSelection = events.currentSelectedGameObject;
                events.SetSelectedGameObject(null);
            }
            _lockedControls = navigation.LobbyControls;
            _lockedControls.interactable = false;
            _lockedControls.blocksRaycasts = false;
            _scroll.StopMovement();
            DomainManager.Instance.OpenPopup("PopupDefenseCard", OnCreated, OnLoadFailed);
        }

        /// <summary>요청한 카드가 남아 있으면 생성된 <paramref name="popup"/>에 현재 값을 반영한다.</summary>
        private void OnCreated(PopupBase popup)
        {
            if (this == null)
            {
                popup.RequestClose();
                return;
            }
            _popup = popup;
            popup.OnClosed += OnClosed;
            if (!isActiveAndEnabled || _game == null || _selectedId == null || _game.FindCard(_selectedId) == null)
            {
                Dismiss();
                return;
            }
            _detail = popup.GetComponent<PopupDefenseCard>();
            _detail.Converted += Converted;
            _detail.Upgraded += Upgraded;
            _detail.Present(_game, _game.FindCard(_selectedId));
        }

        /// <summary>선택이 사라지면 입력을 끊고 상세 창을 닫는다.</summary>
        private void Dismiss()
        {
            _selectedId = null;
            if (_detail != null)
            {
                _detail.Converted -= Converted;
                _detail.Upgraded -= Upgraded;
                _detail = null;
            }
            if (_popup != null && DomainManager.Instance != null && ReferenceEquals(DomainManager.Instance.Peek(), _popup))
            {
                _popup.RequestClose();
            }
        }

        /// <summary>상세 창을 불러오지 못했을 때 다음 선택을 허용한다.</summary>
        private void OnLoadFailed()
        {
            if (this != null)
            {
                if (_selectedId != null)
                {
                    ShowFailure("카드 상세를 열지 못했습니다. 다시 선택해 주세요.");
                }
                Unlock();
            }
        }

        /// <summary><paramref name="popup"/>이 닫히면 목록을 원래 스크롤 위치에서 다시 조작할 수 있게 한다.</summary>
        private void OnClosed(PopupBase popup)
        {
            popup.OnClosed -= OnClosed;
            _popup = null;
            Dismiss();
            Unlock();
        }

        /// <summary>상세 창이 잠갔던 로비 입력을 복구한다.</summary>
        private void Unlock()
        {
            if (_lockedControls != null)
            {
                _lockedControls.interactable = true;
                _lockedControls.blocksRaycasts = true;
                _lockedControls = null;
                if (isActiveAndEnabled && _previousSelection != null && _previousSelection.activeInHierarchy && EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(_previousSelection);
                }
                _previousSelection = null;
            }
        }

        /// <summary>목록의 드래그 <paramref name="eventData"/>가 가로 스와이프이면 기존 로비 탭 전환으로 전달한다.</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            GetComponentInParent<LobbyNavigation>().OnEndDrag(eventData);
        }

        /// <summary>탭을 떠나면 진행 중인 상세 요청과 선택을 해제한다.</summary>
        private void OnDisable() => Dismiss();

        /// <summary>열려 있는 인벤토리에서 카드 상세를 닫고 조작이 실패한 이유 <paramref name="message"/>를 표시한다.</summary>
        public void ShowFailure(string message)
        {
            if (isActiveAndEnabled)
            {
                Dismiss();
                _receipt = message;
                _state.text = message;
                _state.gameObject.SetActive(true);
            }
        }
    }
}
