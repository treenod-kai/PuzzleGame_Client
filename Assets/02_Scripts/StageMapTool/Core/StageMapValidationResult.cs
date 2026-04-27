using System.Collections.Generic;

/// <summary>
/// 스테이지 맵 검증 결과입니다.
/// </summary>
public class StageMapValidationResult
{
    /// <summary> 저장을 막는 오류 목록입니다. </summary>
    public readonly List<string> errors = new List<string>();

    /// <summary> 저장은 가능하지만 확인이 필요한 경고 목록입니다. </summary>
    public readonly List<string> warnings = new List<string>();

    /// <summary>
    /// 저장 가능한 검증 결과인지 확인합니다.
    /// </summary>
    /// <returns>오류가 없으면 true입니다.</returns>
    public bool IsValid()
    {
        return errors.Count == 0;
    }

    /// <summary>
    /// 오류 메시지를 추가합니다.
    /// </summary>
    /// <param name="message">추가할 오류 메시지입니다.</param>
    public void AddError(string message)
    {
        errors.Add(message);
    }

    /// <summary>
    /// 경고 메시지를 추가합니다.
    /// </summary>
    /// <param name="message">추가할 경고 메시지입니다.</param>
    public void AddWarning(string message)
    {
        warnings.Add(message);
    }
}
