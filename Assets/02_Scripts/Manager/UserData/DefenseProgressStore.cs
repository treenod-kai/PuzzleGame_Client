using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using LobbyDefense.Model;
using Puzzle.Core;
using UnityEngine;

/// <summary>현재 계정의 로비 전투와 카드 자산을 파일 하나로 교체하고 정상 저장본을 보존한다.</summary>
internal sealed class DefenseProgressStore
{
    private readonly string _path;
    private bool _recoveredBackup;

    /// <summary><paramref name="owner"/> 전용 저장소를 열고, 확인된 설치 게스트의 기존 진행을 이어받는다.</summary>
    internal DefenseProgressStore(string owner)
    {
        string root = Path.Combine(Application.persistentDataPath, "LobbyDefense");
        string directory = OwnerDirectory(root, owner);
        if (!Directory.Exists(directory) && owner == "uid:" + IdentityLayer.LocalGuestUid)
        {
            string guest = OwnerDirectory(root, "guest:" + IdentityLayer.GetOrCreateGuestProviderUserId());
            if (Directory.Exists(guest))
            {
                Directory.Move(guest, directory);
            }
        }
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "progress.json");
    }

    /// <summary>서버 계정이 확인되면 UID를, 오프라인이면 같은 설치의 게스트 키를 반환한다.</summary>
    internal static string CurrentOwner()
    {
        UserDataManager manager = UserDataManager.Instance;
        string uid = null;
        if (manager != null && manager.Identity != null && manager.Identity.Data != null)
        {
            uid = manager.Identity.Data.uid;
        }
        if (string.IsNullOrEmpty(uid) || uid == "dummy_user")
        {
            uid = IdentityLayer.LocalGuestUid;
        }
        return string.IsNullOrEmpty(uid) ? "guest:" + IdentityLayer.GetOrCreateGuestProviderUserId() : "uid:" + uid;
    }

    /// <summary>경로 문자와 충돌하지 않는 <paramref name="owner"/> 전용 폴더를 <paramref name="root"/> 아래에서 구한다.</summary>
    private static string OwnerDirectory(string root, string owner)
    {
        using (SHA256 hash = SHA256.Create())
        {
            string key = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(owner))).Replace("-", "");
            return Path.Combine(root, key);
        }
    }

    /// <summary><paramref name="rules"/>와 <paramref name="random"/>으로 유효한 저장본을 열고 최초 접속이면 시작 카드를 한 번 저장한다.</summary>
    internal DefenseCampaign Open(DefenseRules rules, PuzzleRandom random)
    {
        if (File.Exists(_path))
        {
            // 파일 잠금 같은 일시적 읽기 오류에서는 원본을 다시 열어 최신 진행을 유지한다.
            string json = File.ReadAllText(_path);
            try
            {
                return Restore(json, rules, random);
            }
            catch (Exception error) when (File.Exists(_path + ".bak"))
            {
                Debug.LogWarning("[로비 디펜스] 이전 저장본으로 복구합니다. " + error.Message);
            }
        }
        if (File.Exists(_path + ".bak"))
        {
            DefenseCampaign recovered = Restore(File.ReadAllText(_path + ".bak"), rules, random);
            _recoveredBackup = true;
            return recovered;
        }
        DefenseCampaign campaign = new DefenseCampaign(rules, random, null);
        Save(campaign);
        return campaign;
    }

    /// <summary>직렬화된 <paramref name="json"/>과 <paramref name="rules"/>, 추첨기 <paramref name="random"/>으로 저장한 전투를 복원한다.</summary>
    internal static DefenseCampaign Restore(string json, DefenseRules rules, PuzzleRandom random)
    {
        DefenseProgress progress = JsonUtility.FromJson<DefenseProgress>(json);
        if (progress == null)
        {
            throw new InvalidDataException("디펜스 저장 데이터가 비어 있습니다.");
        }
        return new DefenseCampaign(rules, random, progress);
    }

    /// <summary><paramref name="campaign"/>을 임시 파일에 기록한 뒤 계정의 마지막 완료 틱과 자산을 한 번에 교체한다.</summary>
    internal void Save(DefenseCampaign campaign)
    {
        string temp = _path + ".tmp";
        File.WriteAllText(temp, JsonUtility.ToJson(campaign.Capture()));
        if (File.Exists(_path))
        {
            File.Replace(temp, _path, _recoveredBackup ? null : _path + ".bak");
        }
        else
        {
            File.Move(temp, _path);
        }
        _recoveredBackup = false;
    }
}
