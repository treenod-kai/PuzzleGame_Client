using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴 셀 표시용 호환 컴포넌트입니다.
/// </summary>
public class StageMapCellView : MonoBehaviour
{
    /// <summary>
    /// 셀 뷰를 초기화합니다.
    /// </summary>
    /// <param name="sprite">셀 배경에 사용할 스프라이트입니다.</param>
    /// <param name="cellSize">셀 하나의 월드 크기입니다.</param>
    public void Initialize(Sprite sprite, float cellSize)
    {
    }

    /// <summary>
    /// 셀 데이터를 반영합니다.
    /// </summary>
    /// <param name="cellData">반영할 셀 데이터입니다.</param>
    public void Refresh(CellData cellData)
    {
    }
}
