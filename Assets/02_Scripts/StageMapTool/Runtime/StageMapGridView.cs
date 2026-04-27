using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴의 그리드 표시를 담당합니다.
/// </summary>
public class StageMapGridView : MonoBehaviour
{
    /// <summary> 셀 하나의 월드 크기입니다. </summary>
    [SerializeField] private float _cellSize = 1f;

    /// <summary> 현재 표시 중인 스테이지 데이터입니다. </summary>
    private StageData _stageData;

    /// <summary>
    /// 스테이지 전체 그리드를 다시 표시합니다.
    /// </summary>
    /// <param name="stageData">표시할 스테이지 데이터입니다.</param>
    public void Rebuild(StageData stageData)
    {
        _stageData = stageData;
        Debug.Log("[StageMapGridView] 그리드 표시 골조가 호출되었습니다.");
    }

    /// <summary>
    /// 지정한 좌표의 셀 표시를 갱신합니다.
    /// </summary>
    /// <param name="x">갱신할 X 좌표입니다.</param>
    /// <param name="y">갱신할 Y 좌표입니다.</param>
    /// <param name="cellData">갱신할 셀 데이터입니다.</param>
    public void RefreshCell(int x, int y, CellData cellData)
    {
        Debug.Log($"[StageMapGridView] 셀 표시 갱신: ({x}, {y}) type: {cellData?.cell_type}");
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환합니다.
    /// </summary>
    /// <param name="worldPosition">변환할 월드 좌표입니다.</param>
    /// <param name="x">변환된 X 좌표입니다.</param>
    /// <param name="y">변환된 Y 좌표입니다.</param>
    /// <returns>보드 범위 안이면 true입니다.</returns>
    public bool TryWorldToGrid(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt(worldPosition.x / _cellSize);
        y = Mathf.FloorToInt(worldPosition.y / _cellSize);

        if (_stageData == null)
        {
            return false;
        }

        return x >= 0 && x < _stageData.stage_width && y >= 0 && y < _stageData.stage_height;
    }
}
