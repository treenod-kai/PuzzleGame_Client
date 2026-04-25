using System.IO;
using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 모드별 스테이지 JSON을 다운로드 경로 또는 Resources에서 로드하는 저장소입니다.
/// </summary>
public static class StageStorage
{
    /// <summary> 스테이지 Resources 루트 경로입니다. </summary>
    private const string StageRoot = "Stage";

    /// <summary>
    /// 퍼즐 타입과 스테이지 번호로 스테이지 JSON을 로드합니다.
    /// </summary>
    /// <param name="puzzleType">스테이지가 속한 퍼즐 모드입니다.</param>
    /// <param name="stageId">로드할 스테이지 번호입니다.</param>
    /// <param name="json">로드된 JSON 문자열입니다.</param>
    /// <returns>스테이지 JSON 로드 성공 여부입니다.</returns>
    public static bool TryLoadStageJson(PuzzleType puzzleType, int stageId, out string json)
    {
        string resourceKey = GetResourceKey(puzzleType, stageId);
        return TryLoadStageJson(resourceKey, out json);
    }

    /// <summary>
    /// 스테이지 리소스 키로 스테이지 JSON을 로드합니다.
    /// </summary>
    /// <param name="resourceKey">Resources 기준 스테이지 키입니다.</param>
    /// <param name="json">로드된 JSON 문자열입니다.</param>
    /// <returns>스테이지 JSON 로드 성공 여부입니다.</returns>
    public static bool TryLoadStageJson(string resourceKey, out string json)
    {
        json = null;

        if (string.IsNullOrEmpty(resourceKey))
        {
            return false;
        }

        string downloadedPath = GetDownloadedPath(resourceKey);
        if (File.Exists(downloadedPath))
        {
            json = File.ReadAllText(downloadedPath);
            return true;
        }

        TextAsset textAsset = Resources.Load<TextAsset>(resourceKey);
        if (textAsset != null)
        {
            json = textAsset.text;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 퍼즐 타입과 스테이지 번호로 Resources 기준 키를 반환합니다.
    /// </summary>
    /// <param name="puzzleType">스테이지가 속한 퍼즐 모드입니다.</param>
    /// <param name="stageId">스테이지 번호입니다.</param>
    /// <returns>Resources.Load에 사용할 스테이지 키입니다.</returns>
    public static string GetResourceKey(PuzzleType puzzleType, int stageId)
    {
        return $"{StageRoot}/{GetModeFolder(puzzleType)}/{GetStageFileName(stageId)}";
    }

    /// <summary>
    /// 퍼즐 타입에 대응하는 스테이지 폴더명을 반환합니다.
    /// </summary>
    /// <param name="puzzleType">폴더명을 구할 퍼즐 모드입니다.</param>
    /// <returns>스테이지 모드 폴더명입니다.</returns>
    public static string GetModeFolder(PuzzleType puzzleType)
    {
        switch (puzzleType)
        {
            case PuzzleType.ThreeMatch:
                return "ThreeMatch";
            case PuzzleType.Link:
                return "Link";
            case PuzzleType.TapMatch:
                return "TapMatch";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// 스테이지 번호에 대응하는 파일명을 반환합니다.
    /// </summary>
    /// <param name="stageId">스테이지 번호입니다.</param>
    /// <returns>확장자를 제외한 스테이지 파일명입니다.</returns>
    public static string GetStageFileName(int stageId)
    {
        return $"Stage_{stageId:000}";
    }

    /// <summary>
    /// 스테이지 리소스 키에 대응하는 다운로드 파일 경로를 반환합니다.
    /// </summary>
    /// <param name="resourceKey">Resources 기준 스테이지 키입니다.</param>
    /// <returns>다운로드 스테이지 JSON 파일 경로입니다.</returns>
    public static string GetDownloadedPath(string resourceKey)
    {
        string normalizedKey = resourceKey.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(Application.persistentDataPath, normalizedKey + ".json");
    }
}

