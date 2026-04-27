using System.Collections.Generic;
using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴 UI 패널의 연결 지점입니다.
/// </summary>
public class StageMapToolPanel : MonoBehaviour
{
    /// <summary> 이벤트를 전달할 툴 컨트롤러입니다. </summary>
    private StageMapToolController _controller;

    /// <summary>
    /// UI 패널을 초기화합니다.
    /// </summary>
    /// <param name="controller">이벤트를 전달할 툴 컨트롤러입니다.</param>
    public void Initialize(StageMapToolController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// 현재 편집 상태를 UI에 반영합니다.
    /// </summary>
    /// <param name="state">표시할 현재 편집 상태입니다.</param>
    public void Refresh(StageMapToolState state)
    {
        Debug.Log($"[StageMapToolPanel] UI 갱신: {state.PuzzleType} Stage_{state.StageId:000}");
    }

    /// <summary>
    /// 검증 결과를 UI에 표시합니다.
    /// </summary>
    /// <param name="result">표시할 검증 결과입니다.</param>
    public void ShowValidationResult(StageMapValidationResult result)
    {
        if (result == null)
        {
            return;
        }

        for (int i = 0; i < result.errors.Count; i++)
        {
            Debug.LogError($"[StageMapToolPanel] {result.errors[i]}");
        }

        for (int i = 0; i < result.warnings.Count; i++)
        {
            Debug.LogWarning($"[StageMapToolPanel] {result.warnings[i]}");
        }
    }

    /// <summary>
    /// 모드 선택 UI 이벤트를 처리합니다.
    /// </summary>
    /// <param name="puzzleTypeValue">선택된 퍼즐 모드 정수 값입니다.</param>
    public void OnChangePuzzleType(int puzzleTypeValue)
    {
        _controller?.ChangePuzzleType((PuzzleType)puzzleTypeValue);
    }

    /// <summary>
    /// 스테이지 번호 UI 이벤트를 처리합니다.
    /// </summary>
    /// <param name="stageId">선택된 스테이지 번호입니다.</param>
    public void OnChangeStageId(int stageId)
    {
        _controller?.ChangeStageId(stageId);
    }

    /// <summary>
    /// 저장 버튼 UI 이벤트를 처리합니다.
    /// </summary>
    public void OnClickSave()
    {
        _controller?.Save();
    }

    /// <summary>
    /// 저장 후 테스트 버튼 UI 이벤트를 처리합니다.
    /// </summary>
    public void OnClickSaveAndTest()
    {
        _controller?.SaveAndTest();
    }

    /// <summary>
    /// 셀 타입 브러시 UI 이벤트를 처리합니다.
    /// </summary>
    /// <param name="cellTypeValue">선택된 셀 타입 정수 값입니다.</param>
    public void OnChangeCellType(int cellTypeValue)
    {
        StageMapCellBrush brush = new StageMapCellBrush
        {
            cellType = (CellType)cellTypeValue
        };
        _controller?.ChangeBrush(brush);
    }

    /// <summary>
    /// 전체 브러시 UI 이벤트를 처리합니다.
    /// </summary>
    /// <param name="cellType">선택된 셀 타입입니다.</param>
    /// <param name="blockId">선택된 블럭 아이디입니다.</param>
    /// <param name="panelId">선택된 패널 아이디입니다.</param>
    /// <param name="generatorBlockIds">선택된 생성기 블럭 아이디 목록입니다.</param>
    public void ApplyBrush(CellType cellType, string blockId, int panelId, List<string> generatorBlockIds)
    {
        StageMapCellBrush brush = new StageMapCellBrush
        {
            cellType = cellType,
            blockId = blockId,
            panelId = panelId
        };

        if (generatorBlockIds != null)
        {
            for (int i = 0; i < generatorBlockIds.Count; i++)
            {
                brush.generatorBlockIds.Add(generatorBlockIds[i]);
            }
        }

        _controller?.ChangeBrush(brush);
    }
}
