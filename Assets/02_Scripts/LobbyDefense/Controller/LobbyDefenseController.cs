using System;
using System.Collections;
using System.Collections.Generic;
using LobbyDefense.Model;
using LobbyDefense.View;
using Puzzle.Core;
using TMPro;
using UnityEngine;

namespace LobbyDefense.Controller
{
    /// <summary>로비의 고정 틱, 계정 교체, 카드 입력과 저장을 관리하고 완료된 상태를 화면에 전달한다.</summary>
    public sealed class LobbyDefenseController : MonoBehaviour
    {
        private DefenseRules _rules;
        private DefenseCampaign _campaign;
        private DefenseProgressStore _store;
        private LobbyDefenseControls _controls;
        private LobbyDefenseView _view;
        private PuzzleRandom _random;
        private string _identityUid;
        private string _owner;
        private readonly Queue<Func<DefenseCampaign, bool>> _pending = new Queue<Func<DefenseCampaign, bool>>();
        private long _savedTick = -1;
        private bool _frozen;
        private bool _saveFailed;
        private bool _ready;
        private float _retryAfter;

        /// <summary>로비 UI의 <paramref name="parent"/>와 인벤토리를 포함한 <paramref name="navigation"/>에 전투 화면을 연결한다.</summary>
        internal void Initialize(RectTransform parent, GameObject navigation)
        {
            Transform content = navigation.transform.Find("SafeArea/OverlayPages/Inventory/Panel/Content");
            if (content == null)
            {
                Debug.LogError("[로비 디펜스] 인벤토리 콘텐츠를 찾을 수 없습니다.");
                return;
            }
            TMP_FontAsset font = content.GetComponentInChildren<TMP_Text>(true).font;
            RectTransform area = (RectTransform)new GameObject("LobbyDefense", typeof(RectTransform)).transform;
            area.SetParent(parent, false);
            area.SetAsFirstSibling();
            area.anchorMin = new Vector2(0.035f, 0.5f);
            area.anchorMax = new Vector2(0.965f, 0.95f);
            area.offsetMin = new Vector2(0f, -210f);
            area.offsetMax = Vector2.zero;
            _view = area.gameObject.AddComponent<LobbyDefenseView>();
            DefenseCardInventoryView inventory = content.gameObject.AddComponent<DefenseCardInventoryView>();
            inventory.Initialize(font);
            LoadRules(font, inventory);
        }

        /// <summary>로비 글꼴 <paramref name="font"/>과 <paramref name="inventory"/>에 사용할 규칙을 비동기로 요청한다.</summary>
        private void LoadRules(TMP_FontAsset font, DefenseCardInventoryView inventory)
        {
            AssetManager.Instance.LoadAssetAsync(new AssetManager.AssetArguments<TextAsset>
            {
                address = DefenseRules.Address,
                successCallback = asset => OpenRules(asset, font, inventory),
                failedCallback = () => OnRulesUnavailable(font, inventory)
            });
        }

        /// <summary>규칙 로드를 기다리는 <paramref name="inventory"/>에 상태를 표시하고 같은 <paramref name="font"/>로 재시도한다.</summary>
        private void OnRulesUnavailable(TMP_FontAsset font, DefenseCardInventoryView inventory)
        {
            if (this != null && inventory != null)
            {
                Debug.LogError("[로비 디펜스] 규칙표를 불러오지 못했습니다. " + DefenseRules.Address);
                inventory.ShowCard(null, "디펜스 데이터 불러오는 중", "잠시 후 다시 시도합니다.", false, false, false);
                StartCoroutine(RetryRules(font, inventory));
            }
        }

        /// <summary>잠시 기다린 뒤 <paramref name="font"/>와 <paramref name="inventory"/>에 연결할 규칙을 다시 요청한다.</summary>
        private IEnumerator RetryRules(TMP_FontAsset font, DefenseCardInventoryView inventory)
        {
            yield return new WaitForSecondsRealtime(3f);
            LoadRules(font, inventory);
        }

        /// <summary>로드한 <paramref name="asset"/>을 검증해 <paramref name="font"/>와 <paramref name="inventory"/>를 전투에 연결한다.</summary>
        private void OpenRules(TextAsset asset, TMP_FontAsset font, DefenseCardInventoryView inventory)
        {
            if (this == null || inventory == null)
            {
                return;
            }
            try
            {
                _rules = JsonUtility.FromJson<DefenseRules>(asset.text);
                if (_rules == null)
                {
                    throw new InvalidOperationException("로비 디펜스 규칙표가 비어 있습니다.");
                }
                _rules.Validate();
                DefenseBoard board = _rules.CreateBoard();
                _view.Initialize(board.Width, board.Height, board.Entrance, board.Exit, font);
                _controls = new LobbyDefenseControls(_view, inventory, Enqueue);
                _random = new PuzzleRandom(Guid.NewGuid().GetHashCode());
                SyncAccount();
            }
            catch (Exception error)
            {
                Debug.LogError("[로비 디펜스] 초기화 실패. " + error);
                inventory.ShowCard(null, "디펜스를 열지 못했습니다", "다시 로비에 들어와 주세요.", false, false, false);
                if (_controls != null)
                {
                    _view.SetControls("", "", false, false, "디펜스 저장 데이터를 불러올 수 없습니다.");
                }
            }
        }

        /// <summary><paramref name="owner"/>의 저장본을 연 뒤에만 활성 계정과 화면을 교체한다.</summary>
        private void OpenAccount(string owner)
        {
            DefenseProgressStore store = new DefenseProgressStore(owner);
            DefenseCampaign campaign = store.Open(_rules, _random);
            _store = store;
            _campaign = campaign;
            _owner = owner;
            _identityUid = CurrentUid();
            _savedTick = _campaign.Session.Tick;
            _saveFailed = false;
            _controls.Bind(_campaign);
            _ready = true;
        }

        /// <summary>로그인·로그아웃 또는 유저 데이터 갱신 후 현재 계정 식별자를 읽는다.</summary>
        private static string CurrentUid()
        {
            UserDataManager manager = UserDataManager.Instance;
            if (manager == null || manager.Identity == null || manager.Identity.Data == null)
            {
                return null;
            }
            return manager.Identity.Data.uid;
        }

        /// <summary>계정 교체 시 이전 진행을 저장하고 해당 계정의 화면과 예약 입력을 닫는다.</summary>
        private bool SyncAccount()
        {
            string uid = CurrentUid();
            if (_ready && _identityUid == uid)
            {
                return true;
            }
            if (!_ready && Time.unscaledTime < _retryAfter)
            {
                return false;
            }
            string owner = DefenseProgressStore.CurrentOwner();
            if (_owner == owner && _campaign != null)
            {
                _identityUid = uid;
                if (!_ready)
                {
                    _controls.Bind(_campaign);
                    _ready = true;
                }
                return true;
            }
            _pending.Clear();
            _ready = false;
            _controls.Unbind("진행 불러오는 중");
            if (!Save())
            {
                return false;
            }
            try
            {
                _campaign = null;
                _owner = null;
                OpenAccount(owner);
                return true;
            }
            catch (Exception error)
            {
                _retryAfter = Time.unscaledTime + 3f;
                _controls.Unbind("진행을 다시 불러오는 중입니다.");
                Debug.LogError("[로비 디펜스] 계정 전환 실패. " + error);
                return false;
            }
        }

        /// <summary>화면에서 확정한 <paramref name="command"/> 한 건을 다음 전투 틱에 실행한다.</summary>
        private void Enqueue(Func<DefenseCampaign, bool> command)
        {
            DefenseCampaign selectedAccount = _campaign;
            if (_ready && !_frozen && SyncAccount() && ReferenceEquals(selectedAccount, _campaign))
            {
                _pending.Enqueue(command);
            }
        }

        /// <summary>입력 직전 상태를 보관해 파일 교체 실패 시 비용과 카드 변경도 함께 되돌린다.</summary>
        private bool CommitPending()
        {
            while (_pending.Count > 0)
            {
                Func<DefenseCampaign, bool> command = _pending.Dequeue();
                string before = JsonUtility.ToJson(_campaign.Capture());
                if (!command(_campaign))
                {
                    _controls.Notify("현재 상태에서 실행할 수 없습니다.");
                    continue;
                }
                if (!Save())
                {
                    _pending.Clear();
                    _campaign = DefenseProgressStore.Restore(before, _rules, _random);
                    _controls.Bind(_campaign);
                    _controls.Notify("저장하지 못해 변경을 취소했습니다.");
                    return false;
                }
                _controls.Notify(null);
            }
            return true;
        }

        /// <summary>입력을 처리한 뒤 1틱 진행하고 처치 보상과 주기적인 이어하기 상태를 저장한다.</summary>
        private void FixedUpdate()
        {
            if (_controls == null || _frozen || !SyncAccount() || !CommitPending())
            {
                return;
            }
            if (_saveFailed && (Time.unscaledTime < _retryAfter || !Save()))
            {
                return;
            }
            int revision = _campaign.Revision;
            _campaign.Step();
            if (revision != _campaign.Revision || _campaign.Session.Tick - _savedTick >= GameSpec.LogicFramesPerSecond * 5)
            {
                Save();
            }
        }

        /// <summary>완료된 틱에서 다음 칸까지 화면 좌표만 보간한다.</summary>
        private void Update()
        {
            if (_ready && SyncAccount())
            {
                _controls.Update();
                float fraction = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
                _view.RenderUnits(_campaign.Session, fraction);
            }
        }

        /// <summary>활성 계정의 같은 시점에 카드와 전투를 쓰며 실패하면 다음 틱까지 진행을 보류한다.</summary>
        private bool Save()
        {
            if (_campaign == null)
            {
                return true;
            }
            try
            {
                _store.Save(_campaign);
                _savedTick = _campaign.Session.Tick;
                _saveFailed = false;
                return true;
            }
            catch (Exception error)
            {
                if (!_saveFailed)
                {
                    Debug.LogError("[로비 디펜스] 저장 실패. " + error);
                    _controls.Notify("저장 공간을 확인해 주세요.");
                }
                _saveFailed = true;
                _retryAfter = Time.unscaledTime + 3f;
                return false;
            }
        }

        /// <summary>대전 진입 전에 입력과 진행을 저장하고 라이프 차감까지 동결한다.</summary>
        internal bool SuspendForBattle()
        {
            if (!_ready || !SyncAccount() || !CommitPending())
            {
                return false;
            }
            DefenseBoard board = _campaign.Session.Board;
            if (board.GetDistance(board.Entrance) < 0)
            {
                _controls.Notify("대전을 시작하려면 입구와 출구 사이의 길을 열어 주세요.");
                return false;
            }
            if (!Save())
            {
                return false;
            }
            _frozen = true;
            return true;
        }

        /// <summary>대전 진입이 취소된 경우 로비 전투를 다시 진행한다.</summary>
        internal void ResumeLobby() => _frozen = false;

        /// <summary>앱이 <paramref name="paused"/> 상태로 전환되면 마지막 완료 틱을 저장한다.</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused && _ready && SyncAccount() && CommitPending())
            {
                Save();
            }
        }

        /// <summary>씬을 떠나거나 플레이가 끝날 때 마무리된 계정 진행을 저장한다.</summary>
        private void OnDestroy()
        {
            if (_ready && !_frozen)
            {
                Save();
            }
        }
    }
}
