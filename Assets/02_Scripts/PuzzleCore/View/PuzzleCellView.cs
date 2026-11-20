using Puzzle.Core;
using UnityEngine;

/// <summary>
/// 개별 퍼즐 셀(배경 타일)의 시각적 표현을 담당하는 클래스입니다.
/// </summary>
public class PuzzleCellView : MonoBehaviour
{
    /// <summary> 셀 이미지를 렌더링할 컴포넌트 </summary>
    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    /// <summary> 셀의 물리 충돌을 담당하는 컴포넌트 </summary>
    [SerializeField]
    private PuzzleCellCollider _boxCollider;

    /// <summary> 외부(Collider 등)에서 스프라이트 정보를 얻기 위한 프로퍼티 </summary>
    public SpriteRenderer SpriteRenderer => _spriteRenderer;

    /// <summary> 연결된 셀 모델 데이터 </summary>
    private PuzzleCell _cellData;

    /// <summary> 보드 내 그리드 좌표 </summary>
    private GridPos _gridPos;

    /// <summary> 관리 중인 보드 뷰 참조 </summary>
    private PuzzleBoardView _boardView;

    /// <summary> 이 셀의 타일 스프라이트에 적용할 보드 모양입니다. </summary>
    private BoardShape _boardShape;

    /// <summary> 풀 재사용 시 이전 셀의 이미지 요청을 구별하는 번호입니다. </summary>
    private ulong _visualRequestVersion;

    /// <summary> 비활성화한 셀의 이미지 요청을 무효화합니다. </summary>
    private void OnDisable()
    {
        _visualRequestVersion++;
    }

    /// <summary>
    /// 셀 뷰를 특정 모델 데이터 및 좌표로 초기화합니다.
    /// </summary>
    /// <param name="cellData">연결할 셀 모델 객체</param>
    /// <param name="pos">배치될 그리드 좌표</param>
    /// <param name="boardView">보드 뷰 객체</param>
    /// <param name="boardShape">명시할 보드 모양입니다. 생략하면 소속 보드의 모양을 사용합니다.</param>
    public void Initialize(PuzzleCell cellData, GridPos pos, PuzzleBoardView boardView, BoardShape boardShape = BoardShape.None)
    {
        _cellData = cellData;
        _gridPos = pos;
        _boardView = boardView;
        _boardShape = boardShape;
        if (_boardShape == BoardShape.None)
        {
            if (boardView != null)
            {
                _boardShape = boardView.Shape;
            }
            else
            {
                GameSpec spec = StageInjection.Instance.GetGameSpec();
                _boardShape = spec == null ? BoardShape.Quadrangle : spec.rule.boardShape;
            }
        }

        UpdateVisual();
    }

    /// <summary>
    /// 셀의 상태에 따라 시각적 요소(Sprite 등)를 업데이트합니다.
    /// 육각형(Hexagon) 보드인 경우 육각형 전용 스프라이트를 로드합니다.
    /// </summary>
    public void UpdateVisual()
    {
        ulong requestVersion = ++_visualRequestVersion;
        if (_cellData == null)
        {
            return;
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (_spriteRenderer != null)
        {
            switch (_cellData.CellType)
            {
                case CellType.Normal:
                case CellType.Generator:
                    _spriteRenderer.gameObject.SetActive(true);

                    // 리플레이도 자신의 격자에 맞는 타일을 사용합니다.
                    string spriteAddress = _boardShape == BoardShape.Hexagon ? "hexagonCell" : "squareCell";
                    _spriteRenderer.sprite = null;

                    /// <summary> 셀 스프라이트와 충돌체 크기를 갱신합니다. </summary>
                    /// <param name="sprite">로드한 셀 스프라이트</param>
                    void OnCellSpriteLoaded(Sprite sprite)
                    {
                        if (this != null && _spriteRenderer != null && sprite != null && requestVersion == _visualRequestVersion)
                        {
                            _spriteRenderer.sprite = sprite;

                            // 스프라이트 변경 후 콜라이더 크기 재조정
                            if (_boxCollider != null)
                            {
                                _boxCollider.AdjustColliderSize();
                            }
                        }
                    }

                    // Addressables 비동기 로드를 통해 스프라이트 교체
                    AssetManager.Instance.LoadAssetAsync<Sprite>(new AssetManager.AssetArguments<Sprite>()
                    {
                        address = spriteAddress,
                        successCallback = OnCellSpriteLoaded
                    });
                    break;

                case CellType.Close:
                    _spriteRenderer.gameObject.SetActive(false);
                    break;
            }
        }
    }

    /// <summary>
    /// 보드 뷰 등에 의해 이 셀이 클릭되었다고 판정되었을 때 호출됩니다.
    /// </summary>
    public void OnClicked()
    {
        if (_boardView == null || _cellData == null)
        {
            return;
        }

        // Generator이거나 Normal일 때 입력을 받도록 수정 (링크 연결 시작 지점으로 Generator 포함 가능성)
        if (_cellData.CellType == CellType.Normal || _cellData.CellType == CellType.Generator)
        {
            _boardView.OnBlockInput(_gridPos);
        }
    }
}
