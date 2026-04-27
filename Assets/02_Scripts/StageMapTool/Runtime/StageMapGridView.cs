using Puzzle.Core;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스테이지 맵 툴의 그리드 표시를 담당합니다.
/// </summary>
public class StageMapGridView : MonoBehaviour
{
    /// <summary> 셀 하나의 월드 크기입니다. </summary>
    [SerializeField] private float _cellSize = 1f;

    /// <summary> 현재 표시 중인 스테이지 데이터입니다. </summary>
    private StageData _stageData;

    /// <summary> 생성된 셀 뷰 목록입니다. </summary>
    private readonly List<StageMapCellView> _cellViews = new List<StageMapCellView>();

    /// <summary> 셀 배경 표시용 기본 스프라이트입니다. </summary>
    private Sprite _cellSprite;

    /// <summary> 공용 카메라 프레이밍 완료 여부입니다. </summary>
    private bool _isCameraFocused;

    /// <summary>
    /// SharedScene 카메라가 늦게 준비되는 경우 보드 프레이밍을 재시도합니다.
    /// </summary>
    private void Update()
    {
        if (!_isCameraFocused)
        {
            _isCameraFocused = FocusSharedCamera();
        }
    }

    /// <summary>
    /// 스테이지 전체 그리드를 다시 표시합니다.
    /// </summary>
    /// <param name="stageData">표시할 스테이지 데이터입니다.</param>
    public void Rebuild(StageData stageData)
    {
        _stageData = stageData;
        ClearCells();
        _isCameraFocused = false;

        if (_stageData == null || _stageData.cells == null)
        {
            return;
        }

        EnsureCellSprite();
        for (int i = 0; i < _stageData.cells.Count; i++)
        {
            CellData cellData = _stageData.cells[i];
            if (cellData == null)
            {
                continue;
            }

            StageMapCellView cellView = CreateCellView(cellData);
            cellView.Refresh(cellData);
            _cellViews.Add(cellView);
        }

        _isCameraFocused = FocusSharedCamera();
    }

    /// <summary>
    /// 지정한 좌표의 셀 표시를 갱신합니다.
    /// </summary>
    /// <param name="x">갱신할 X 좌표입니다.</param>
    /// <param name="y">갱신할 Y 좌표입니다.</param>
    /// <param name="cellData">갱신할 셀 데이터입니다.</param>
    public void RefreshCell(int x, int y, CellData cellData)
    {
        StageMapCellView cellView = FindCellView(x, y);
        if (cellView != null)
        {
            cellView.Refresh(cellData);
        }
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
        if (_stageData == null)
        {
            x = 0;
            y = 0;
            return false;
        }

        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        x = Mathf.FloorToInt(localPosition.x / _cellSize + _stageData.stage_width * 0.5f);
        y = Mathf.FloorToInt(localPosition.y / _cellSize + _stageData.stage_height * 0.5f);
        return x >= 0 && x < _stageData.stage_width && y >= 0 && y < _stageData.stage_height;
    }

    /// <summary>
    /// 기존 셀 뷰를 모두 제거합니다.
    /// </summary>
    private void ClearCells()
    {
        for (int i = 0; i < _cellViews.Count; i++)
        {
            if (_cellViews[i] != null)
            {
                Destroy(_cellViews[i].gameObject);
            }
        }

        _cellViews.Clear();
    }

    /// <summary>
    /// 셀 배경에 사용할 기본 스프라이트를 보장합니다.
    /// </summary>
    private void EnsureCellSprite()
    {
        if (_cellSprite != null)
        {
            return;
        }

        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _cellSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    /// <summary>
    /// 셀 데이터에 대응하는 셀 뷰를 생성합니다.
    /// </summary>
    /// <param name="cellData">생성할 셀 데이터입니다.</param>
    /// <returns>생성된 셀 뷰입니다.</returns>
    private StageMapCellView CreateCellView(CellData cellData)
    {
        GameObject cellObject = new GameObject($"Cell_{cellData.x:00}_{cellData.y:00}");
        cellObject.transform.SetParent(transform, false);
        cellObject.transform.localPosition = GetCellLocalPosition(cellData.x, cellData.y);

        StageMapCellView cellView = cellObject.AddComponent<StageMapCellView>();
        cellView.Initialize(_cellSprite, _cellSize);
        return cellView;
    }

    /// <summary>
    /// 그리드 좌표에 대응하는 로컬 좌표를 반환합니다.
    /// </summary>
    /// <param name="x">변환할 X 좌표입니다.</param>
    /// <param name="y">변환할 Y 좌표입니다.</param>
    /// <returns>셀 중심 로컬 좌표입니다.</returns>
    private Vector3 GetCellLocalPosition(int x, int y)
    {
        float localX = (x + 0.5f - _stageData.stage_width * 0.5f) * _cellSize;
        float localY = (y + 0.5f - _stageData.stage_height * 0.5f) * _cellSize;
        return new Vector3(localX, localY, 0f);
    }

    /// <summary>
    /// 지정한 좌표의 셀 뷰를 찾습니다.
    /// </summary>
    /// <param name="x">찾을 X 좌표입니다.</param>
    /// <param name="y">찾을 Y 좌표입니다.</param>
    /// <returns>좌표에 대응하는 셀 뷰입니다.</returns>
    private StageMapCellView FindCellView(int x, int y)
    {
        string targetName = $"Cell_{x:00}_{y:00}";
        for (int i = 0; i < _cellViews.Count; i++)
        {
            StageMapCellView cellView = _cellViews[i];
            if (cellView != null && cellView.gameObject.name == targetName)
            {
                return cellView;
            }
        }

        return null;
    }

    /// <summary>
    /// SharedScene의 공용 카메라가 보드를 볼 수 있도록 위치와 크기를 맞춥니다.
    /// </summary>
    private bool FocusSharedCamera()
    {
        Camera camera = CameraController.MainCamera;
        if (camera == null || _stageData == null)
        {
            return false;
        }

        float boardWidth = _stageData.stage_width * _cellSize;
        float boardHeight = _stageData.stage_height * _cellSize;
        float aspect = camera.aspect > 0f ? camera.aspect : 1f;
        float heightSize = boardHeight * 0.5f + _cellSize;
        float widthSize = boardWidth / aspect * 0.5f + _cellSize;

        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(heightSize, widthSize);
        camera.transform.position = new Vector3(transform.position.x, transform.position.y, camera.transform.position.z);
        return true;
    }
}
