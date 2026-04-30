using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴의 좌표 변환과 셀 생성 후보 표시를 담당합니다.
/// </summary>
public class StageMapGridView : MonoBehaviour
{
    /// <summary> 셀 생성 후보 프리팹의 Addressables 주소입니다. </summary>
    private const string CellMakerAddress = "CellMaker";

    /// <summary> 셀 하나의 월드 크기입니다. </summary>
    [SerializeField] private float _cellSize = 1f;

    /// <summary> 셀 생성 후보 간격입니다. </summary>
    [SerializeField] private float _cellMakerSpacing = 0.5f;

    /// <summary> 가로 최대 셀 개수입니다. </summary>
    [SerializeField] private int _maxCellWidth = 10;

    /// <summary> 세로 최대 셀 개수입니다. </summary>
    [SerializeField] private int _maxCellHeight = 10;

    /// <summary> 현재 편집 중인 스테이지 데이터입니다. </summary>
    private StageData _stageData;

    /// <summary> 셀 생성 후보의 부모 트랜스폼입니다. </summary>
    private Transform _cellMakerRoot;

    /// <summary> 로드한 셀 생성 후보 프리팹입니다. </summary>
    private GameObject _cellMakerPrefab;

    /// <summary>
    /// 스테이지 좌표 변환 기준 데이터를 갱신합니다.
    /// </summary>
    /// <param name="stageData">좌표 변환에 사용할 스테이지 데이터입니다.</param>
    public void Rebuild(StageData stageData)
    {
        _stageData = stageData;
        RebuildCellMakers();
    }

    /// <summary>
    /// 지정한 좌표의 셀 갱신 요청을 처리합니다.
    /// </summary>
    /// <param name="x">갱신할 X 좌표입니다.</param>
    /// <param name="y">갱신할 Y 좌표입니다.</param>
    /// <param name="cellData">갱신할 셀 데이터입니다.</param>
    public void RefreshCell(int x, int y, CellData cellData)
    {
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
    /// 셀 생성 후보를 다시 배치합니다.
    /// </summary>
    private void RebuildCellMakers()
    {
        ClearCellMakers();
        EnsureCellMakerRoot();
        EnsureCellMakerPrefab();

        if (_cellMakerPrefab == null)
        {
            Debug.LogError($"[StageMapGridView] 셀 생성 후보 프리팹 로드 실패: {CellMakerAddress}");
            return;
        }

        int horizontalCount = Mathf.FloorToInt((_maxCellWidth - 1) * _cellSize / _cellMakerSpacing) + 1;
        int verticalCount = Mathf.FloorToInt((_maxCellHeight - 1) * _cellSize / _cellMakerSpacing) + 1;
        float startX = -(_maxCellWidth * _cellSize * 0.5f) + (_cellSize * 0.5f);
        float startY = -(_maxCellHeight * _cellSize * 0.5f) + (_cellSize * 0.5f);

        for (int y = 0; y < verticalCount; y++)
        {
            for (int x = 0; x < horizontalCount; x++)
            {
                GameObject cellMaker = Instantiate(_cellMakerPrefab, _cellMakerRoot);
                cellMaker.name = $"CellMaker_{x:00}_{y:00}";
                cellMaker.transform.localPosition = new Vector3(startX + x * _cellMakerSpacing, startY + y * _cellMakerSpacing, 0f);
                cellMaker.transform.localRotation = Quaternion.identity;
            }
        }
    }

    /// <summary>
    /// 셀 생성 후보 부모를 보장합니다.
    /// </summary>
    private void EnsureCellMakerRoot()
    {
        if (_cellMakerRoot != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("CellMakerRoot");
        rootObject.transform.SetParent(transform, false);
        _cellMakerRoot = rootObject.transform;
    }

    /// <summary>
    /// 셀 생성 후보 프리팹을 보장합니다.
    /// </summary>
    private void EnsureCellMakerPrefab()
    {
        if (_cellMakerPrefab != null)
        {
            return;
        }

        _cellMakerPrefab = AssetManager.Instance.LoadAsset<GameObject>(CellMakerAddress);
    }

    /// <summary>
    /// 기존 셀 생성 후보를 제거합니다.
    /// </summary>
    private void ClearCellMakers()
    {
        if (_cellMakerRoot == null)
        {
            return;
        }

        for (int i = _cellMakerRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_cellMakerRoot.GetChild(i).gameObject);
        }
    }
}
