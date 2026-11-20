using System;
using System.Collections.Generic;
using LobbyDefense.Model;
using LobbyDefense.View;
using Puzzle.Core;
using UnityEngine;

namespace LobbyDefense.Controller
{
    /// <summary>로비와 인벤토리에서 선택한 카드·후보 칸을 유지하고 화면 입력을 전투 명령에 연결한다.</summary>
    internal sealed class LobbyDefenseControls
    {
        private readonly LobbyDefenseView _view;
        private readonly DefenseCardInventoryView _inventory;
        private readonly Action<Func<DefenseCampaign, bool>> _submit;
        private readonly List<int> _route = new List<int>();
        private readonly List<int> _preview = new List<int>();
        private DefenseCampaign _game;
        private int _cell = -1;
        private int _choice;
        private int _revision;
        private int _boardRevision;
        private int _wave;
        private long _updatedTick;
        private int _seconds = -1;
        private bool _canPlace;
        private bool _enemyBlocked;
        private bool _preparing;
        private bool _nextIsGhost;
        private string _message;

        /// <summary><paramref name="view"/>와 <paramref name="inventory"/>의 입력을 다음 틱의 <paramref name="submit"/>에 연결한다.</summary>
        internal LobbyDefenseControls(LobbyDefenseView view, DefenseCardInventoryView inventory, Action<Func<DefenseCampaign, bool>> submit)
        {
            _view = view;
            _inventory = inventory;
            _submit = submit;
            view.CellSelected += SelectCell;
            view.ChoiceChanged += SelectChoice;
            view.Confirmed += Confirm;
            view.Recalled += Recall;
            inventory.Converted += ConvertCard;
            inventory.Upgraded += Upgrade;
        }

        /// <summary>새로 연 <paramref name="game"/>의 카드·배치·진행 상태를 표시한다.</summary>
        internal void Bind(DefenseCampaign game)
        {
            _game = game;
            _view.BindCombat(game);
            _cell = -1;
            _choice = 0;
            _message = null;
            _view.gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>계정 교체 중 이전 유저의 지도와 카드를 숨기고 <paramref name="message"/>를 인벤토리에 표시한다.</summary>
        internal void Unbind(string message)
        {
            _game = null;
            _view.BindCombat(null);
            _view.gameObject.SetActive(false);
            _inventory.ShowState(message);
        }

        /// <summary>조작 결과 <paramref name="message"/>를 지도에 표시하고, <paramref name="showInventoryFailure"/>이면 열린 인벤토리에도 실패를 알린다.</summary>
        internal void Notify(string message, bool showInventoryFailure = false)
        {
            _message = message;
            if (_game != null)
            {
                Refresh();
                if (showInventoryFailure)
                {
                    _inventory.ShowFailure(message);
                }
            }
        }

        private int HireIndex => _choice - _game.Cards.Count;
        private bool IsCard => HireIndex < 0;
        private bool IsTrap => HireIndex == _game.Rules.Units.Length;
        private int Count => _game.Cards.Count + _game.Rules.Units.Length + 1;

        /// <summary>선택 항목을 <paramref name="direction"/>만큼 순환해 보유·고용·트랩 후보를 표시한다.</summary>
        private void SelectChoice(int direction)
        {
            _choice = (_choice + direction + Count) % Count;
            _message = null;
            Refresh();
        }

        /// <summary>누른 <paramref name="cell"/>을 미리보기 후보로 선택한다. 배치된 유닛을 누르면 그 카드를 선택한다.</summary>
        private void SelectCell(int cell)
        {
            _cell = cell;
            _message = null;
            foreach (DeployedDefender defender in _game.Session.Defenders)
            {
                if (defender.Cell == cell)
                {
                    _view.RequestShowcase(defender, _game.Session.Tick);
                    for (int i = 0; i < _game.Cards.Count; i++)
                    {
                        if (_game.Cards[i].Id == defender.Unit.CardInstanceId)
                        {
                            _choice = i;
                            break;
                        }
                    }
                    break;
                }
            }
            Refresh();
        }

        /// <summary>선택된 카드의 지도 위치를 구한다. 미배치 또는 고용·트랩 후보는 -1을 반환한다.</summary>
        private int SelectedLocation()
        {
            if (IsCard)
            {
                string id = _game.Cards[_choice].Id;
                foreach (DeployedDefender defender in _game.Session.Defenders)
                {
                    if (defender.Unit.CardInstanceId == id)
                    {
                        return defender.Cell;
                    }
                }
            }
            return -1;
        }

        /// <summary>현재 선택의 소유권·비용·정비 상태·침공자 예약 칸을 확인한다.</summary>
        private bool CanConfirm()
        {
            DefenseSession session = _game.Session;
            if (IsTrap)
            {
                return session.Traps.Count < _game.Rules.TrapLimit && session.CanPlaceTrap(_cell);
            }
            if (!session.CanPlace(_cell))
            {
                return false;
            }
            if (!IsCard)
            {
                return _game.CanHire(_game.Rules.Units[HireIndex].Id);
            }
            OwnedDefenseCard card = _game.Cards[_choice];
            return _game.IsRecovered(card) && (session.IsPreparing || !session.IsCardDeployed(card.Id));
        }

        /// <summary>확정한 칸과 카드 ID를 고정해 다음 틱의 처리 중 선택이 바뀌어도 원래 항목을 적용한다.</summary>
        private void Confirm()
        {
            if (!CanConfirm())
            {
                return;
            }
            int cell = _cell;
            if (IsTrap)
            {
                _submit(game => game.Session.Traps.Count < game.Rules.TrapLimit && game.Session.TryPlaceTrap(cell));
            }
            else if (IsCard)
            {
                string id = _game.Cards[_choice].Id;
                _submit(game => game.TryPlace(id, cell));
            }
            else
            {
                string type = _game.Rules.Units[HireIndex].Id;
                _submit(game => game.TryHire(type, cell));
            }
        }

        /// <summary>선택한 배치 카드를 정비 중 회수한다.</summary>
        private void Recall()
        {
            if (IsCard)
            {
                string id = _game.Cards[_choice].Id;
                _submit(game => game.TryRecall(id));
            }
        }

        /// <summary>확정한 카드 <paramref name="id"/> 한 장을 해당 종류의 에너지로 변환한다.</summary>
        private void ConvertCard(string id) => _submit(game => game.TryConvert(id));

        /// <summary>카드 <paramref name="id"/>의 <paramref name="stat"/> 하나를 같은 종류의 에너지로 강화한다.</summary>
        private void Upgrade(string id, DefenseAttribute stat) => _submit(game => game.TryUpgrade(id, stat));

        /// <summary>지도나 카드가 바뀌었을 때 선택 상태와 경로 및 인벤토리 표시를 함께 갱신한다.</summary>
        internal void Refresh()
        {
            DefenseSession session = _game.Session;
            DefenseBoard board = session.Board;
            _choice = Mathf.Clamp(_choice, 0, Count - 1);
            _revision = _game.Revision;
            _boardRevision = board.Revision;
            _wave = session.Wave;
            _updatedTick = session.Tick;
            _preparing = session.IsPreparing;
            _canPlace = CanConfirm();
            int location = SelectedLocation();
            board.GetPath(board.Entrance, _route);
            _preview.Clear();
            bool blocked = false;
            if (!IsTrap && board.CanPlace(_cell))
            {
                board.TryPreview(_cell, location, _preview, out blocked);
            }
            _enemyBlocked = PreviewBlocksEnemy();
            string choice = "마비 트랩";
            string action = "설치";
            int range = 0;
            if (!IsTrap)
            {
                DefenseUnitDefinition unit = IsCard ? _game.Rules.Find(_game.Cards[_choice].TypeId) : _game.Rules.Units[HireIndex];
                range = unit.Range;
                choice = IsCard ? $"{unit.Name}  카드 {_choice + 1}" : $"{unit.Name}  {_game.Rules.HireGold} 골드";
                action = IsCard ? (location >= 0 ? "이동" : "배치") : "고용";
            }
            string hint = _message;
            if (string.IsNullOrEmpty(hint))
            {
                hint = !_game.Session.HasStarted ? "등장 구역 밖에 첫 카드를 놓아 웨이브를 시작하세요." : "좌우로 드래그하고 배치할 칸을 누르세요.";
                if (_cell >= 0)
                {
                    if (board.IsSpawnArea(_cell))
                    {
                        hint = "몬스터 등장 구역입니다. 유닛과 트랩은 바깥에 놓으세요.";
                    }
                    else if (blocked || _enemyBlocked)
                    {
                        hint = "길이 막히면 침공 몬스터가 방어 유닛을 공격합니다.";
                    }
                    else if (location >= 0 && !_preparing)
                    {
                        hint = "배치된 유닛은 정비 시간에 이동하거나 회수할 수 있습니다.";
                    }
                    else if (!_canPlace)
                    {
                        hint = "현재 선택으로 이 칸에 배치할 수 없습니다.";
                    }
                }
            }
            _view.SetControls(choice, action, _canPlace, _preparing && location >= 0, hint);
            _view.SetRoute(_route, _preview, _cell, range, _canPlace);
            _inventory.ShowInventory(_game);
            SetStatus(SecondsRemaining());
        }

        /// <summary>다음 소환이나 정비 종료까지 남은 시간을 논리 틱에서 초로 변환한다.</summary>
        private int SecondsRemaining()
        {
            DefenseSession session = _game.Session;
            long next = NextSpawnIsGhost() ? session.NextGhostSpawnTick : session.NextNormalSpawnTick;
            if (session.IsPreparing)
            {
                next = session.NextWaveTick;
            }
            if (next < 0)
            {
                return -1;
            }
            return (int)((next - session.Tick + GameSpec.LogicFramesPerSecond - 1) / GameSpec.LogicFramesPerSecond);
        }

        /// <summary>다음 일반 몬스터와 유령 중 유령이 먼저 소환되는지 확인한다.</summary>
        private bool NextSpawnIsGhost()
        {
            DefenseSession session = _game.Session;
            return session.NextGhostSpawnTick >= 0 && (session.NextNormalSpawnTick < 0 ||
                session.NextGhostSpawnTick < session.NextNormalSpawnTick);
        }

        /// <summary>웨이브 상태와 다음 전환까지 <paramref name="seconds"/>초를 표시한다.</summary>
        private void SetStatus(int seconds)
        {
            _seconds = seconds;
            _nextIsGhost = NextSpawnIsGhost();
            string wave = _wave == 0 ? "첫 배치 대기" : $"웨이브 {_wave}";
            string spawn = "";
            if (_preparing)
            {
                spawn = $"정비 {seconds}초";
            }
            else if (seconds >= 0)
            {
                spawn = _nextIsGhost ? $"유령 {seconds}초" : $"일반 {seconds}초";
            }
            _view.SetStatus(_game.Gold, wave, spawn);
        }

        /// <summary>배치 후보가 입구와 떨어진 몬스터의 현재 칸이나 다음 도착 칸을 고립시키는지도 확인한다.</summary>
        private bool PreviewBlocksEnemy()
        {
            if (_preview.Count == 0)
            {
                return false;
            }
            DefenseSession session = _game.Session;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                InvadingMonster enemy = session.Enemies[i];
                if (session.Board.IsBlockedInPreview(enemy.Cell) || session.Board.IsBlockedInPreview(enemy.NextCell))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>지도·예약 칸·잔액·웨이브 또는 초 단위 표시가 바뀐 경우에만 화면을 갱신한다.</summary>
        internal void Update()
        {
            DefenseSession session = _game.Session;
            if (_revision != _game.Revision || _boardRevision != session.Board.Revision || _wave != session.Wave ||
                _preparing != session.IsPreparing)
            {
                Refresh();
                return;
            }
            if (_updatedTick == session.Tick)
            {
                return;
            }
            _updatedTick = session.Tick;
            if (_canPlace != CanConfirm() || _enemyBlocked != PreviewBlocksEnemy())
            {
                Refresh();
                return;
            }
            int seconds = SecondsRemaining();
            if (seconds != _seconds || _nextIsGhost != NextSpawnIsGhost())
            {
                SetStatus(seconds);
            }
        }
    }
}
