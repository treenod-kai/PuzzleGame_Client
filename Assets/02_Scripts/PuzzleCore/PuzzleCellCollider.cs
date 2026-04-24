using UnityEngine;

/// <summary>
/// 유니티 내장 물리/이벤트(마우스 클릭 등)를 감지하여 셀 뷰(PuzzleCellView)로 전달하는 역할을 합니다.
/// </summary>
public class PuzzleCellCollider : MonoBehaviour
{
    /// <summary> 이 콜라이더가 속한 셀 뷰 </summary>
    [SerializeField]
    private PuzzleCellView _cell;

    /// <summary> 충돌체 컴포넌트 </summary>
    [SerializeField]
    private BoxCollider2D _boxCollider2D;

    /// <summary>
    /// 마우스 또는 터치로 BoxCollider2D 영역이 클릭되었을 때 호출됩니다.
    /// </summary>
    private void OnMouseDown()
    {
        OnClickCell();
    }

    /// <summary>
    /// 뷰 쪽에 클릭 이벤트를 전달합니다.
    /// </summary>
    internal void OnClickCell()
    {
        if (_cell != null)
        {
            _cell.OnClicked();
        }
    }

    /// <summary>
    /// 연결된 뷰의 스프라이트 크기에 맞춰 BoxCollider2D의 크기를 자동으로 조절합니다.
    /// </summary>
    public void AdjustColliderSize()
    {
        if (_cell != null && _cell.SpriteRenderer != null && _cell.SpriteRenderer.sprite != null)
        {
            if (_boxCollider2D != null)
            {
                _boxCollider2D.size = _cell.SpriteRenderer.sprite.bounds.size;
            }
        }
    }
}
