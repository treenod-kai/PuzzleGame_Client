using UnityEngine;

/// <summary>
/// 유니티 내장 물리/이벤트(마우스 클릭 등)를 감지하여 블럭 뷰(PuzzleBlockView)로 전달하는 역할을 합니다.
/// </summary>
public class PuzzleBlockCollider : MonoBehaviour
{
    /// <summary> 이 콜라이더가 속한 블럭 뷰 </summary>
    [SerializeField]
    private PuzzleBlockView _block;

    /// <summary> 충돌체 컴포넌트 </summary>
    [SerializeField]
    private BoxCollider2D _boxCollider2D;

    /// <summary>
    /// 블럭이 클릭되었을 때 호출되어 뷰의 클릭 로직을 실행합니다.
    /// </summary>
    internal void OnClickBlock()
    {
        if (_block != null)
        {
            _block.OnClicked();
        }
    }

    /// <summary>
    /// 연결된 뷰의 스프라이트 크기에 맞춰 BoxCollider2D의 크기를 자동으로 조절합니다.
    /// </summary>
    public void AdjustColliderSize()
    {
        if (_block != null && _block.SpriteRenderer != null && _block.SpriteRenderer.sprite != null)
        {
            if (_boxCollider2D != null)
            {
                _boxCollider2D.size = _block.SpriteRenderer.sprite.bounds.size;
            }
        }
    }
}
