using System;
using PuzleBattleShared.Models;
using UnityEngine;

/// <summary>
/// 유저 기본 정보 레이어.
/// 서버에서 유저 데이터를 요청하고 보관합니다.
/// DTO는 PuzleBattleShared.dll의 UserData를 사용합니다.
/// </summary>
public class IdentityLayer
{
    /// <summary> 서버 엔드포인트 경로 </summary>
    private const string LOGIN_ENDPOINT = "auth/login";
    private const string GuestProviderUserIdKey = "PuzleBattle.GuestProviderUserId";
    private const string GuestUidKey = "PuzleBattle.LocalGuestUid";

    /// <summary>이 설치의 게스트 자격으로 서버에서 확인한 계정 ID다.</summary>
    internal static string LocalGuestUid => PlayerPrefs.GetString(GuestUidKey, "");

    /// <summary>이 설치의 게스트 ID <paramref name="providerUserId"/>로 로그인한 서버 계정 <paramref name="uid"/>를 기억한다.</summary>
    internal static void RememberGuest(string providerUserId, string uid)
    {
        if (!string.IsNullOrEmpty(uid) && uid != "dummy_user" &&
            providerUserId == GetOrCreateGuestProviderUserId() && uid != LocalGuestUid)
        {
            PlayerPrefs.SetString(GuestUidKey, uid);
            PlayerPrefs.Save();
        }
    }

    /// <summary> 유저 기본 데이터 (PuzleBattleShared.Models.UserData) </summary>
    public UserData Data { get; private set; }

    /// <summary> 데이터가 서버에서 로드되었는지 여부 </summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// 서버에서 유저 데이터를 요청합니다.
    /// 서버 연결 실패 시 더미 데이터로 폴백합니다.
    /// </summary>
    /// <param name="onComplete">로드 완료 시 호출되는 콜백</param>
    /// <param name="onFailed">로드 실패 시 호출되는 콜백</param>
    public void Request(Action onComplete, Action<string> onFailed)
    {
        AuthLoginRequest request = new AuthLoginRequest
        {
            provider = AuthProvider.Guest,
            providerUserId = GetOrCreateGuestProviderUserId(),
            nickname = "Guest",
            email = ""
        };

        /// <summary>로그인 응답에서 게스트 정보를 적용합니다.</summary>
        void OnIdentityLoaded(AuthLoginResponse response)
        {
            if (response == null || response.user == null)
            {
                OnIdentityLoadFailed("게스트 로그인 응답에 유저 정보가 없습니다.");
                return;
            }

            RememberGuest(request.providerUserId, response.user.uid);
            SetData(response.user);
            Debug.Log($"[IdentityLayer] 게스트 로그인 완료: uid={Data.uid}, nickname={Data.nickname}");
            if (onComplete != null)
            {
                onComplete.Invoke();
            }
        }

        /// <summary>서버 요청에 실패하면 로컬 게스트 정보를 적용합니다.</summary>
        void OnIdentityLoadFailed(string error)
        {
            Debug.LogWarning($"[IdentityLayer] 서버 연결 실패, 더미 데이터로 대체: {error}");
            SetData(CreateDummyData());
            if (onComplete != null)
            {
                onComplete.Invoke();
            }
        }

        NetworkManager.Instance.Post<AuthLoginRequest, AuthLoginResponse>(
            LOGIN_ENDPOINT, request, OnIdentityLoaded, OnIdentityLoadFailed);
    }

    /// <summary>
    /// 로그인/연동 응답으로 받은 유저 데이터를 반영합니다.
    /// </summary>
    public void SetData(UserData data)
    {
        Data = data;
        IsLoaded = data != null;
    }

    /// <summary>
    /// 서버 연결 실패 시 사용할 더미 데이터를 생성합니다.
    /// </summary>
    /// <returns>더미 유저 데이터</returns>
    private UserData CreateDummyData()
    {
        return new UserData
        {
            uid = "dummy_user",
            nickname = "Guest",
            freeCoin = 1000,
            paidCoin = 0,
            freeDia = 50,
            paidDia = 0
        };
    }

    /// <summary>이 설치에서 오프라인 진행과 서버 게스트 로그인에 함께 사용할 식별자를 얻는다.</summary>
    internal static string GetOrCreateGuestProviderUserId()
    {
        string providerUserId = PlayerPrefs.GetString(GuestProviderUserIdKey, "");
        if (!string.IsNullOrEmpty(providerUserId))
        {
            return providerUserId;
        }

        providerUserId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(GuestProviderUserIdKey, providerUserId);
        PlayerPrefs.Save();
        return providerUserId;
    }

    /// <summary>
    /// 데이터를 초기화합니다. 로그아웃 시 호출합니다.
    /// </summary>
    public void Clear()
    {
        Data = null;
        IsLoaded = false;
    }
}
