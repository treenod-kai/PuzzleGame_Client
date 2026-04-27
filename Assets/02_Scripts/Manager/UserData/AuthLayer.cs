using System;
using PuzleBattleShared.Models;
using UnityEngine;

/// <summary>
/// 로그인 및 계정 연동 레이어.
/// 게스트, 구글, 애플, 페이스북 계정과 서버 유저 데이터를 연결합니다.
/// </summary>
public class AuthLayer
{
    private const string LOGIN_ENDPOINT = "auth/login";
    private const string LINK_ENDPOINT = "auth/link";

    public AuthLoginResponse LastResponse { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public void Login(
        AuthProvider provider,
        string providerUserId,
        string nickname,
        string email,
        string uid,
        Action<AuthLoginResponse> onComplete,
        Action<string> onFailed)
    {
        AuthLoginRequest request = new AuthLoginRequest
        {
            provider = provider,
            providerUserId = providerUserId,
            uid = uid,
            nickname = nickname,
            email = email
        };

        NetworkManager.Instance.Post<AuthLoginRequest, AuthLoginResponse>(
            LOGIN_ENDPOINT,
            request,
            (response) =>
            {
                LastResponse = response;
                IsLoggedIn = true;
                Debug.Log($"[AuthLayer] 로그인 완료: provider={provider}, uid={response.user.uid}");
                onComplete?.Invoke(response);
            },
            onFailed
        );
    }

    public void Link(
        string uid,
        AuthProvider provider,
        string providerUserId,
        string email,
        Action<AuthLoginResponse> onComplete,
        Action<string> onFailed)
    {
        AuthLoginRequest request = new AuthLoginRequest
        {
            uid = uid,
            provider = provider,
            providerUserId = providerUserId,
            email = email
        };

        NetworkManager.Instance.Post<AuthLoginRequest, AuthLoginResponse>(
            LINK_ENDPOINT,
            request,
            (response) =>
            {
                LastResponse = response;
                IsLoggedIn = true;
                Debug.Log($"[AuthLayer] 계정 연동 완료: provider={provider}, uid={response.user.uid}");
                onComplete?.Invoke(response);
            },
            onFailed
        );
    }

    public void Clear()
    {
        LastResponse = null;
        IsLoggedIn = false;
    }
}
