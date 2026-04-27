using System.Collections.Generic;
using Puzzle.Core;

/// <summary>
/// 스테이지 맵 툴에서 셀에 적용할 브러시 상태입니다.
/// </summary>
public class StageMapCellBrush
{
    /// <summary> 적용할 셀 타입입니다. </summary>
    public CellType cellType = CellType.Normal;

    /// <summary> 적용할 초기 블럭 아이디입니다. </summary>
    public string blockId;

    /// <summary> 적용할 패널 아이디입니다. </summary>
    public int panelId;

    /// <summary> 생성기 셀에 적용할 생성 블럭 아이디 목록입니다. </summary>
    public readonly List<string> generatorBlockIds = new List<string>();

    /// <summary>
    /// 다른 브러시의 값을 현재 브러시에 복사합니다.
    /// </summary>
    /// <param name="source">복사할 원본 브러시입니다.</param>
    public void CopyFrom(StageMapCellBrush source)
    {
        if (source == null)
        {
            return;
        }

        cellType = source.cellType;
        blockId = source.blockId;
        panelId = source.panelId;

        generatorBlockIds.Clear();
        for (int i = 0; i < source.generatorBlockIds.Count; i++)
        {
            generatorBlockIds.Add(source.generatorBlockIds[i]);
        }
    }
}
