using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 스테이지 맵 툴에서 단일 셀의 시각 표시를 담당합니다.
/// </summary>
public class StageMapCellView : MonoBehaviour
{
    /// <summary> 셀 배경 스프라이트 렌더러입니다. </summary>
    private SpriteRenderer _backgroundRenderer;

    /// <summary> 셀 정보 라벨입니다. </summary>
    private TextMesh _label;

    /// <summary> 현재 표시 중인 X 좌표입니다. </summary>
    private int _x;

    /// <summary> 현재 표시 중인 Y 좌표입니다. </summary>
    private int _y;

    /// <summary>
    /// 셀 뷰를 초기화합니다.
    /// </summary>
    /// <param name="sprite">셀 배경에 사용할 스프라이트입니다.</param>
    /// <param name="cellSize">셀 하나의 월드 크기입니다.</param>
    public void Initialize(Sprite sprite, float cellSize)
    {
        CreateBackground(sprite, cellSize);
        CreateLabel(cellSize);
    }

    /// <summary>
    /// 셀 데이터를 시각 상태에 반영합니다.
    /// </summary>
    /// <param name="cellData">표시할 셀 데이터입니다.</param>
    public void Refresh(CellData cellData)
    {
        if (cellData == null)
        {
            return;
        }

        _x = cellData.x;
        _y = cellData.y;
        _backgroundRenderer.color = GetCellColor((CellType)cellData.cell_type);
        _label.text = BuildLabel(cellData);
    }

    /// <summary>
    /// 셀 배경 오브젝트를 생성합니다.
    /// </summary>
    /// <param name="sprite">배경 스프라이트입니다.</param>
    /// <param name="cellSize">셀 하나의 월드 크기입니다.</param>
    private void CreateBackground(Sprite sprite, float cellSize)
    {
        _backgroundRenderer = gameObject.AddComponent<SpriteRenderer>();
        _backgroundRenderer.sprite = sprite;
        _backgroundRenderer.sortingOrder = 0;
        transform.localScale = Vector3.one * (cellSize * 0.92f);
    }

    /// <summary>
    /// 셀 라벨 오브젝트를 생성합니다.
    /// </summary>
    /// <param name="cellSize">셀 하나의 월드 크기입니다.</param>
    private void CreateLabel(float cellSize)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        _label = labelObject.AddComponent<TextMesh>();
        _label.anchor = TextAnchor.MiddleCenter;
        _label.alignment = TextAlignment.Center;
        _label.characterSize = cellSize * 0.12f;
        _label.fontSize = 28;
        _label.color = Color.black;

        MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 1;
        }
    }

    /// <summary>
    /// 셀 타입에 대응하는 표시 색상을 반환합니다.
    /// </summary>
    /// <param name="cellType">표시할 셀 타입입니다.</param>
    /// <returns>셀 배경 색상입니다.</returns>
    private Color GetCellColor(CellType cellType)
    {
        switch (cellType)
        {
            case CellType.Close:
                return new Color(0.18f, 0.18f, 0.18f, 1f);
            case CellType.Lock:
                return new Color(0.88f, 0.58f, 0.28f, 1f);
            case CellType.Generator:
                return new Color(0.35f, 0.72f, 0.95f, 1f);
            case CellType.Normal:
            default:
                return new Color(0.86f, 0.9f, 0.92f, 1f);
        }
    }

    /// <summary>
    /// 셀 데이터 표시용 라벨 문자열을 생성합니다.
    /// </summary>
    /// <param name="cellData">라벨로 표시할 셀 데이터입니다.</param>
    /// <returns>셀 라벨 문자열입니다.</returns>
    private string BuildLabel(CellData cellData)
    {
        string blockText = string.IsNullOrEmpty(cellData.block_id) ? "-" : cellData.block_id;
        string generatorText = string.Empty;
        if (cellData.generator_block_ids != null && cellData.generator_block_ids.Count > 0)
        {
            generatorText = $"\nG:{cellData.generator_block_ids.Count}";
        }

        return $"{_x},{_y}\n{blockText}\nP:{cellData.panel_id}{generatorText}";
    }
}
