using System.Collections.Generic;
using Puzzle.Core;

/// <summary>
/// 스테이지 맵 툴씬의 현재 편집 상태입니다.
/// </summary>
public class StageMapToolState
{
    /// <summary> 현재 편집 중인 퍼즐 모드입니다. </summary>
    public PuzzleType PuzzleType { get; private set; } = PuzzleType.ThreeMatch;

    /// <summary> 현재 편집 중인 스테이지 번호입니다. </summary>
    public int StageId { get; private set; } = StageStorage.MinStageId;

    /// <summary> 현재 선택된 규칙 Addressable 주소입니다. </summary>
    public string RuleAddress { get; private set; }

    /// <summary> 현재 편집 중인 스테이지 데이터입니다. </summary>
    public StageData StageData { get; private set; }

    /// <summary> 현재 선택된 셀 브러시입니다. </summary>
    public StageMapCellBrush Brush { get; } = new StageMapCellBrush();

    /// <summary> 현재 편집 중인 스테이지 데이터가 있는지 여부입니다. </summary>
    public bool HasStageData
    {
        get
        {
            return StageData != null;
        }
    }

    /// <summary>
    /// 현재 편집 컨텍스트를 설정합니다.
    /// </summary>
    /// <param name="puzzleType">편집할 퍼즐 모드입니다.</param>
    /// <param name="stageId">편집할 스테이지 번호입니다.</param>
    /// <param name="ruleAddress">사용할 규칙 Addressable 주소입니다.</param>
    public void SetContext(PuzzleType puzzleType, int stageId, string ruleAddress)
    {
        PuzzleType = puzzleType;
        StageId = stageId;
        RuleAddress = ruleAddress;
    }

    /// <summary>
    /// 현재 편집 퍼즐 모드와 규칙 주소를 변경합니다.
    /// </summary>
    /// <param name="puzzleType">변경할 퍼즐 모드입니다.</param>
    /// <param name="ruleAddress">변경할 규칙 Addressable 주소입니다.</param>
    public void SetPuzzleType(PuzzleType puzzleType, string ruleAddress)
    {
        PuzzleType = puzzleType;
        RuleAddress = ruleAddress;
    }

    /// <summary>
    /// 현재 편집 중인 스테이지 데이터를 교체합니다.
    /// </summary>
    /// <param name="stageData">편집 대상으로 사용할 스테이지 데이터입니다.</param>
    public void SetStage(StageData stageData)
    {
        StageData = stageData;
    }

    /// <summary>
    /// 현재 브러시 값을 교체합니다.
    /// </summary>
    /// <param name="brush">복사할 브러시 값입니다.</param>
    public void SetBrush(StageMapCellBrush brush)
    {
        Brush.CopyFrom(brush);
    }

    /// <summary>
    /// 지정한 좌표의 셀 데이터를 반환합니다.
    /// </summary>
    /// <param name="x">조회할 X 좌표입니다.</param>
    /// <param name="y">조회할 Y 좌표입니다.</param>
    /// <returns>좌표에 대응하는 셀 데이터입니다.</returns>
    public CellData GetCell(int x, int y)
    {
        if (StageData == null || StageData.cells == null)
        {
            return null;
        }

        for (int i = 0; i < StageData.cells.Count; i++)
        {
            CellData cell = StageData.cells[i];
            if (cell != null && cell.x == x && cell.y == y)
            {
                return cell;
            }
        }

        return null;
    }

    /// <summary>
    /// 지정한 좌표의 셀에 현재 브러시를 적용합니다.
    /// </summary>
    /// <param name="x">수정할 X 좌표입니다.</param>
    /// <param name="y">수정할 Y 좌표입니다.</param>
    /// <returns>브러시 적용 성공 여부입니다.</returns>
    public bool PaintCell(int x, int y)
    {
        CellData cell = GetCell(x, y);
        if (cell == null)
        {
            return false;
        }

        cell.cell_type = (int)Brush.cellType;
        cell.panel_id = Brush.panelId;

        if (Brush.cellType == CellType.Close)
        {
            cell.block_id = null;
            EnsureGeneratorList(cell).Clear();
            return true;
        }

        cell.block_id = Brush.blockId;
        List<string> generatorIds = EnsureGeneratorList(cell);
        generatorIds.Clear();

        if (Brush.cellType == CellType.Generator)
        {
            for (int i = 0; i < Brush.generatorBlockIds.Count; i++)
            {
                generatorIds.Add(Brush.generatorBlockIds[i]);
            }
        }

        return true;
    }

    /// <summary>
    /// 셀의 생성기 목록을 보장하고 반환합니다.
    /// </summary>
    /// <param name="cell">생성기 목록을 확인할 셀입니다.</param>
    /// <returns>셀의 생성기 블럭 아이디 목록입니다.</returns>
    private static List<string> EnsureGeneratorList(CellData cell)
    {
        if (cell.generator_block_ids == null)
        {
            cell.generator_block_ids = new List<string>();
        }

        return cell.generator_block_ids;
    }
}
