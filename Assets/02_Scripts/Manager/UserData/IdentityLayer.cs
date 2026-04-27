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

        NetworkManager.Instance.Post<AuthLoginRequest, AuthLoginResponse>(
            LOGIN_ENDPOINT,
            request,
            (response) =>
            {
                Data = response.user;
                IsLoaded = true;
                Debug.Log($"[IdentityLayer] 게스트 로그인 완료: uid={Data.uid}, nickname={Data.nickname}");
                onComplete?.Invoke();
            },
            (error) =>
            {
                Debug.LogWarning($"[IdentityLayer] 서버 연결 실패, 더미 데이터로 대체: {error}");
                Data = CreateDummyData();
                IsLoaded = true;
                onComplete?.Invoke();
            }
        );
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

    private string GetOrCreateGuestProviderUserId()
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
