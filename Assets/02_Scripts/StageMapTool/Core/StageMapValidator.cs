using System.Collections.Generic;
using Puzzle.Core;

/// <summary>
/// 스테이지 맵 저장 전 데이터 정합성을 검증합니다.
/// </summary>
public class StageMapValidator
{
    /// <summary>
    /// 스테이지 데이터와 규칙 블럭 목록을 검증합니다.
    /// </summary>
    /// <param name="stageData">검증할 스테이지 데이터입니다.</param>
    /// <param name="stageId">현재 파일 번호 기준 스테이지 번호입니다.</param>
    /// <param name="ruleBlocks">현재 규칙에서 허용하는 블럭 목록입니다.</param>
    /// <returns>검증 결과입니다.</returns>
    public StageMapValidationResult Validate(StageData stageData, int stageId, List<BlockData> ruleBlocks)
    {
        StageMapValidationResult result = new StageMapValidationResult();
        if (stageData == null)
        {
            result.AddError("스테이지 데이터가 없습니다.");
            return result;
        }

        if (stageData.stage_id != stageId)
        {
            result.AddError($"파일 번호와 stage_id가 다릅니다. 파일 번호: {stageId}, stage_id: {stageData.stage_id}");
        }

        if (stageData.stage_width <= 0 || stageData.stage_height <= 0)
        {
            result.AddError($"스테이지 크기가 올바르지 않습니다. width: {stageData.stage_width}, height: {stageData.stage_height}");
            return result;
        }

        if (stageData.cells == null)
        {
            result.AddError("셀 목록이 없습니다.");
            return result;
        }

        ValidateCells(stageData, ruleBlocks, result);
        return result;
    }

    /// <summary>
    /// 셀 목록의 좌표, 누락, 블럭 아이디를 검증합니다.
    /// </summary>
    /// <param name="stageData">검증할 스테이지 데이터입니다.</param>
    /// <param name="ruleBlocks">현재 규칙에서 허용하는 블럭 목록입니다.</param>
    /// <param name="result">검증 결과를 누적할 객체입니다.</param>
    private void ValidateCells(StageData stageData, List<BlockData> ruleBlocks, StageMapValidationResult result)
    {
        bool[,] occupied = new bool[stageData.stage_width, stageData.stage_height];
        int validCellCount = 0;

        for (int i = 0; i < stageData.cells.Count; i++)
        {
            CellData cell = stageData.cells[i];
            if (cell == null)
            {
                result.AddError($"셀 데이터가 null입니다. index: {i}");
                continue;
            }

            if (cell.x < 0 || cell.x >= stageData.stage_width || cell.y < 0 || cell.y >= stageData.stage_height)
            {
                result.AddError($"셀 좌표가 범위를 벗어났습니다. x: {cell.x}, y: {cell.y}");
                continue;
            }

            if (occupied[cell.x, cell.y])
            {
                result.AddError($"셀 좌표가 중복되었습니다. x: {cell.x}, y: {cell.y}");
                continue;
            }

            occupied[cell.x, cell.y] = true;
            validCellCount++;
            ValidateCellPayload(cell, ruleBlocks, result);
        }

        int expectedCount = stageData.stage_width * stageData.stage_height;
        if (validCellCount != expectedCount)
        {
            result.AddError($"셀 개수가 보드 크기와 다릅니다. 현재: {validCellCount}, 필요: {expectedCount}");
        }
    }

    /// <summary>
    /// 단일 셀의 타입별 필드 값을 검증합니다.
    /// </summary>
    /// <param name="cell">검증할 셀 데이터입니다.</param>
    /// <param name="ruleBlocks">현재 규칙에서 허용하는 블럭 목록입니다.</param>
    /// <param name="result">검증 결과를 누적할 객체입니다.</param>
    private void ValidateCellPayload(CellData cell, List<BlockData> ruleBlocks, StageMapValidationResult result)
    {
        CellType cellType = (CellType)cell.cell_type;
        if (cellType == CellType.Close)
        {
            if (!string.IsNullOrEmpty(cell.block_id))
            {
                result.AddError($"Close 셀에 블럭이 남아 있습니다. x: {cell.x}, y: {cell.y}, block_id: {cell.block_id}");
            }

            if (cell.generator_block_ids != null && cell.generator_block_ids.Count > 0)
            {
                result.AddError($"Close 셀에 생성 목록이 남아 있습니다. x: {cell.x}, y: {cell.y}");
            }

            return;
        }

        if (!string.IsNullOrEmpty(cell.block_id) && !ContainsBlock(ruleBlocks, cell.block_id))
        {
            result.AddError($"Rule에 없는 block_id입니다. x: {cell.x}, y: {cell.y}, block_id: {cell.block_id}");
        }

        if (cellType == CellType.Generator)
        {
            if (cell.generator_block_ids == null || cell.generator_block_ids.Count == 0)
            {
                result.AddError($"Generator 셀의 생성 목록이 비어 있습니다. x: {cell.x}, y: {cell.y}");
                return;
            }

            for (int i = 0; i < cell.generator_block_ids.Count; i++)
            {
                string blockId = cell.generator_block_ids[i];
                if (!ContainsBlock(ruleBlocks, blockId))
                {
                    result.AddError($"Rule에 없는 generator_block_ids 항목입니다. x: {cell.x}, y: {cell.y}, block_id: {blockId}");
                }
            }
        }
    }

    /// <summary>
    /// 규칙 블럭 목록에 대상 블럭 아이디가 포함되어 있는지 확인합니다.
    /// </summary>
    /// <param name="ruleBlocks">현재 규칙에서 허용하는 블럭 목록입니다.</param>
    /// <param name="blockId">확인할 블럭 아이디입니다.</param>
    /// <returns>블럭 아이디가 존재하면 true입니다.</returns>
    private bool ContainsBlock(List<BlockData> ruleBlocks, string blockId)
    {
        if (ruleBlocks == null || string.IsNullOrEmpty(blockId))
        {
            return false;
        }

        for (int i = 0; i < ruleBlocks.Count; i++)
        {
            BlockData blockData = ruleBlocks[i];
            if (blockData != null && blockData.blockId == blockId)
            {
                return true;
            }
        }

        return false;
    }
}
