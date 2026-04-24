/// <summary>
/// 도메인 항목의 종류를 정의합니다.
/// </summary>
public enum DomainType
{
    /// <summary> 팝업 도메인 (스택 방식, 열고 닫을 때 생성/파괴) </summary>
    Popup,

    /// <summary> 탭 도메인 (전환 방식, 활성/비활성으로 전환) </summary>
    Tab
}

/// <summary>
/// 도메인 경로의 각 노드를 나타내는 인터페이스.
/// 팝업(PopupBase)과 탭(TabBase) 모두 이 인터페이스를 구현합니다.
/// </summary>
public interface IDomainNode
{
    /// <summary> 도메인 경로에서 사용되는 이름 </summary>
    string DomainName { get; }

    /// <summary> 도메인 항목의 종류 (팝업 또는 탭) </summary>
    DomainType DomainType { get; }
}
