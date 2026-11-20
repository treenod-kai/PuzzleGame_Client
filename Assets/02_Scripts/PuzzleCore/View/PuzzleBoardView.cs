using UnityEngine;
using System.Collections.Generic;

using Puzzle.Core;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 퍼즐 보드의 데이터(Model)를 받아 화면에 시각적으로 렌더링하는 View 클래스입니다.
/// 보드의 크기에 맞춰 타일과 블럭들을 배치하고 관리합니다.
/// </summary>
public class PuzzleBoardView : MonoBehaviour
{
    /// <summary> 생성할 셀(배경 타일)의 Addressables 주소 </summary>
    private string cellAddress = "CellPrefab";

    /// <summary> 생성할 블럭의 Addressables 주소 </summary>
    private string blockAddress = "BlockPrefab";

    [Header("Hierarchy Roots")]
    /// <summary> 셀들이 생성될 부모 트랜스폼 </summary>
    public Transform cellRoot;

    /// <summary> 블럭들이 생성될 부모 트랜스폼 </summary>
    public Transform blockRoot;

    [Header("Settings")]
    /// <summary> 타일 한 칸의 시각적 크기 </summary>
    public float cellSize = 1.0f;

    /// <summary> 보드 외곽의 여백 크기 </summary>
    public float padding = 1.0f;

    /// <summary> true이면 DrawBoard 시 카메라 orthographicSize 조정을 건너뜁니다. (리플레이 보드용) </summary>
    public bool skipCameraAlign = false;

    /// <summary> 
    /// 보드의 수직 위치 오프셋 (0: 중앙, 0.5: 상단 정렬, -0.5: 하단 정렬) 
    /// </summary>
    [Range(-0.5f, 0.5f)]
    public float offsetY = 0f;

    /// <summary> 씬 뷰에서 그리드 좌표와 블럭 정보를 표시할지 여부 </summary>
    public bool showDebugGrid = true;

    /// <summary> 현재 연결된 보드 모델 데이터 </summary>
    private IPuzzleBoard _board;

    /// <summary> 사용자 입력 전달 허용 여부입니다. 리플레이 보드는 비활성화합니다. </summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary> 캐싱된 보드 모양 (GetLocalPos 최적화용) </summary>
    private BoardShape _cachedBoardShape = BoardShape.None;

    /// <summary> 이 보드를 그릴 때 지정한 사각 또는 육각 격자 모양입니다. </summary>
    public BoardShape Shape => _cachedBoardShape;

    /// <summary> 로드된 셀 프리팹 캐시 </summary>
    private GameObject _cellPrefabObj;

    /// <summary> 로드된 블럭 프리팹 캐시 </summary>
    private GameObject _blockPrefabObj;
    
    /// <summary> 화면에 생성된 셀 뷰들을 좌표별로 관리하는 딕셔너리 </summary>
    private Dictionary<GridPos, PuzzleCellView> _cellViews = new Dictionary<GridPos, PuzzleCellView>();

    /// <summary> 화면에 생성된 블럭 뷰들을 좌표별로 관리하는 딕셔너리 </summary>
    private Dictionary<GridPos, PuzzleBlockView> _blockViews = new Dictionary<GridPos, PuzzleBlockView>();

    /// <summary> 보드 연출 액션 그룹 대기열 (프레임 단위로 그룹화됨) </summary>
    private Queue<List<BoardViewAction>> _actionQueue = new Queue<List<BoardViewAction>>();

    /// <summary> 액션 그룹 리스트 재사용 풀 </summary>
    private readonly Stack<List<BoardViewAction>> _actionGroupPool = new Stack<List<BoardViewAction>>();

    /// <summary> 액션 그룹 사이 대기 시간 캐시 </summary>
    private static readonly WaitForSeconds _actionGroupDelay = new WaitForSeconds(0.019f);

    /// <summary> 현재 연출이 진행 중인지 여부 </summary>
    private bool _isAnimating = false;

    /// <summary> 드래그 연결 궤적을 표시하는 라인 렌더러 </summary>
    private LineRenderer _lineRenderer;

    /// <summary> LineRenderer에 사용되는 머티리얼 (해제 시 파괴 필요) </summary>
    private Material _lineMaterial;

    /// <summary> 링크 경로 위 연결 지점 표시용 포인트 오브젝트 풀 </summary>
    private List<GameObject> _linkPointMarkers = new List<GameObject>();

    /// <summary> 포인트 마커용 프로시저럴 원형 텍스처 </summary>
    private Texture2D _circleTexture;

    /// <summary> 포인트 마커용 스프라이트 </summary>
    private Sprite _circleSprite;

    /// <summary> 마지막으로 화면에 반영한 링크 경로의 좌표 복사본입니다. </summary>
    private readonly List<GridPos> _previousLinkPath = new List<GridPos>();

    private const int MaxMatchFragments = 24;
    private readonly List<SpriteRenderer> _matchFragments = new List<SpriteRenderer>(MaxMatchFragments);
    private LineRenderer _linkRing;
    private Tween _linkTipTravel;
    private Tween _linkTipPulse;
    private static readonly Color _linkTrailColor = new Color(0.22f, 0.86f, 0.85f);
    private static readonly Color _readyTrailColor = new Color(1f, 0.76f, 0.31f);
    private readonly List<LineRenderer> _bombWaves = new List<LineRenderer>();
    private readonly List<LineRenderer> _pivotOutlines = new List<LineRenderer>();
    private static readonly Color _pivotGold = new Color(1f, 0.82f, 0.35f);
    private static readonly Color _pivotCore = new Color(1f, 0.99f, 0.83f);
    private static readonly Color _pivotAqua = new Color(0.40f, 0.98f, 0.94f);
    /// <summary>한 덩어리로 이어진 보드의 외곽점과 그 둘레에서 재사용할 피벗 연출입니다.</summary>
    private sealed class PivotContour
    {
        public readonly List<Vector3> Points = new List<Vector3>();
        public readonly LineRenderer[] Trails = new LineRenderer[2];
        public LineRenderer Border;
        public LineRenderer Ignition;
        public Bounds LocalBounds;
    }

    private readonly List<PivotContour> _pivotContours = new List<PivotContour>();
    private int _pivotContourCount;
    // 아래쪽 변부터 반시계 방향. 열 간격은 한 칸, 짝수 열의 중심은 반 칸 아래입니다.
    private static readonly Vector2[] _hexCorners =
    {
        new Vector2(-1f / 3f, -0.5f), new Vector2(1f / 3f, -0.5f), new Vector2(2f / 3f, 0f),
        new Vector2(1f / 3f, 0.5f), new Vector2(-1f / 3f, 0.5f), new Vector2(-2f / 3f, 0f)
    };
    private static readonly GridPos[] _evenHexNeighbors =
    {
        new GridPos(0, -1), new GridPos(1, -1), new GridPos(1, 0),
        new GridPos(0, 1), new GridPos(-1, 0), new GridPos(-1, -1)
    };
    private ParticleSystem _pivotSparks;
    private Mesh _pivotStarMesh;
    private float _pivotTravel;
    private const float LinkRingWidth = 0.035f;
    private const float PivotBorderWidth = 0.065f;
    private const float PivotTrailWidth = 0.14f;
    private const float PivotNeighborWidth = 0.072f;
    private float WorldCellSize => cellSize * Mathf.Abs(transform.lossyScale.x);
    private bool _pivotActive;

    /// <summary> 현재 보드가 애니메이션 연출 중인지 여부를 반환합니다. </summary>
    public bool IsAnimating => _isAnimating;

#if UNITY_EDITOR
    /// <summary> 디버그 기즈모용 캐싱된 GUIStyle (매 프레임 할당 방지) </summary>
    private static GUIStyle _debugStyle;

    /// <summary>
    /// 유니티 에디터의 씬 뷰에서 그리드 좌표와 블럭 정보를 텍스트로 표시합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showDebugGrid || _board == null || _board.Cells == null)
        {
            return;
        }

        if (_debugStyle == null)
        {
            _debugStyle = new GUIStyle();
            _debugStyle.normal.textColor = Color.yellow;
            _debugStyle.fontSize = 12;
            _debugStyle.alignment = TextAnchor.MiddleCenter;
        }
        GUIStyle style = _debugStyle;

        foreach (KeyValuePair<GridPos, PuzzleCell> kvp in _board.Cells)
        {
            GridPos pos = kvp.Key;
            PuzzleCell cell = kvp.Value;
            
            Transform root = cellRoot != null ? cellRoot : transform;
            Vector3 worldPos = root.TransformPoint(GetLocalPos(pos));
            
            string info = $"({pos.X},{pos.Y})";

            if (_blockViews.TryGetValue(pos, out PuzzleBlockView blockView))
            {
                Block blockData = blockView.GetBlockData();
                string blockId = null;
                if (blockData != null)
                {
                    blockId = blockData.GetBlockId();
                }
                if (blockId == null)
                {
                    blockId = "NullData";
                }
                info += $"\nID: {blockId}";
            }
            else if (cell.Block != null)
            {
                if (_board.State == BoardState.Waiting)
                {
                    info += $"\nID: {cell.Block.GetBlockId()}\n[MISSING VIEW]";
                }
                else
                {
                    info += $"\nID: {cell.Block.GetBlockId()}\n[PROCESSING]";
                }
            }
            else if (cell.CellType == CellType.Generator)
            {
                info += "\n[GEN]";
            }
            else
            {
                info += "\n[EMPTY]";
            }

            UnityEditor.Handles.Label(worldPos, info, style);
            Gizmos.color = new Color(1, 1, 1, 0.2f);
            Gizmos.DrawWireCube(worldPos, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));
        }
    }
#endif

    /// <summary> 보드가 화면에서 사라지면 진행 중인 일회성 효과를 정리합니다.</summary>
    private void OnDisable()
    {
        ClearFeedback();
        HideAllLinkPointMarkers();
        _previousLinkPath.Clear();
        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 0;
        }
    }

    /// <summary> 코루틴을 중단하고 애니메이션 상태와 링크 표시 리소스를 정리합니다. </summary>
    private void OnDestroy()
    {
        StopAllCoroutines();
        _isAnimating = false;

        if (_pivotStarMesh != null)
        {
            Destroy(_pivotStarMesh);
        }
        if (_lineMaterial != null)
        {
            Destroy(_lineMaterial);
            _lineMaterial = null;
        }
        if (_circleTexture != null)
        {
            Destroy(_circleTexture);
            _circleTexture = null;
        }
        if (_circleSprite != null)
        {
            Destroy(_circleSprite);
            _circleSprite = null;
        }
    }

    /// <summary>
    /// 보드 데이터를 바탕으로 셀과 블럭을 화면에 그립니다.
    /// </summary>
    /// <param name="boardData">그릴 보드 모델</param>
    /// <param name="boardShape">보드 모양 (null이면 StageInjection에서 조회)</param>
    public void DrawBoard(IPuzzleBoard boardData, BoardShape? boardShape = null)
    {
        _board = boardData;

        // 보드 모양 캐싱 (GetLocalPos에서 매번 StageInjection 조회 방지)
        if (boardShape.HasValue)
        {
            _cachedBoardShape = boardShape.Value;
        }
        else
        {
            GameSpec spec = StageInjection.Instance.GetGameSpec();
            _cachedBoardShape = BoardShape.None;
            if (spec != null)
            {
                _cachedBoardShape = spec.rule.boardShape;
            }
        }

        if (_board.Cells == null)
        {
            return;
        }

        _cellPrefabObj = AssetManager.Instance.LoadAsset<GameObject>(cellAddress);
        _blockPrefabObj = AssetManager.Instance.LoadAsset<GameObject>(blockAddress);

        if (_cellPrefabObj == null || _blockPrefabObj == null)
        {
            Debug.LogError($"[PuzzleBoardView] Failed to load prefabs. Cell: {cellAddress}, Block: {blockAddress}");
            return;
        }

        ClearBoard();
        _pivotContourCount = 0;
        SetupLineRenderer();

        if (cellRoot == null)
        {
            cellRoot = this.transform;
        }
        if (blockRoot == null)
        {
            blockRoot = this.transform;
        }

        foreach (KeyValuePair<GridPos, PuzzleCell> kvp in _board.Cells)
        {
            GridPos gridPos = kvp.Key;
            PuzzleCell cell = kvp.Value;

            CreateCellView(gridPos, cell);

            if (cell.Block != null)
            {
                CreateBlockView(gridPos, cell.Block);
            }
        }

        if (!skipCameraAlign)
        {
            AlignBoardToCenter();
        }
    }

    /// <summary> 링크 경로를 표시할 선과 마커 리소스를 준비합니다. </summary>
    private void SetupLineRenderer()
    {
        if (_lineRenderer == null)
        {
            GameObject lineObj = new GameObject("LinkLine");
            lineObj.transform.SetParent(transform, false);
            _lineRenderer = lineObj.AddComponent<LineRenderer>();
            _lineRenderer.startWidth = 25f;
            _lineRenderer.endWidth = 25f;
            _lineRenderer.positionCount = 0;
            _lineRenderer.useWorldSpace = false;
            _lineRenderer.sortingLayerName = "Ingame";
            _lineRenderer.sortingOrder = 100;

            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }

            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                _lineMaterial = new Material(shader);
                _lineRenderer.material = _lineMaterial;
            }
            else
            {
                Debug.LogError("[PuzzleBoardView] LineRenderer용 셰이더를 찾을 수 없습니다.");
            }

            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.numCapVertices = 4;
            _lineRenderer.startColor = _linkTrailColor;
            _lineRenderer.endColor = Color.white;
        }
        _lineRenderer.positionCount = 0;

        // 포인트 마커용 원형 스프라이트 생성
        if (_circleSprite == null)
        {
            _circleTexture = CreateCircleTexture(64, Color.white);
            _circleSprite = Sprite.Create(
                _circleTexture,
                new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f),
                1f
            );
        }
    }

    /// <summary>
    /// 프로시저럴 방식으로 원형 텍스처를 생성합니다.
    /// </summary>
    /// <param name="size">텍스처의 가로/세로 크기 (픽셀)</param>
    /// <param name="color">원의 색상</param>
    /// <returns>생성된 원형 텍스처</returns>
    private Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius)
                {
                    tex.SetPixel(x, y, color);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary> 격자 좌표를 보드 형태에 맞는 로컬 위치로 변환합니다. </summary>
    /// <param name="pos">대상 격자 좌표</param>
    public Vector3 GetLocalPos(GridPos pos)
    {
        if (_board == null)
        {
            return Vector3.zero;
        }

        float offsetX = (_board.Width - 1) * cellSize / 2f;
        float offsetY = (_board.Height - 1) * cellSize / 2f;

        float xPos = pos.X * cellSize - offsetX;
        float yPos = pos.Y * cellSize - offsetY;

        // 육각형(Hexagon) 오프셋 처리 (Even-Q Flat-Top)
        if (_cachedBoardShape == BoardShape.Hexagon)
        {
            if (pos.X % 2 == 0)
            {
                yPos -= cellSize * 0.5f; // 짝수 열은 반 칸 아래로 배치
            }
        }

        return new Vector3(xPos, yPos, 0);
    }

    /// <summary> 보드를 화면 중앙에 배치하고 카메라 크기를 조정합니다. </summary>
    private void AlignBoardToCenter()
    {
        if (_board == null)
        {
            return;
        }

        // 육각형 보드일 때 짝수 열의 반 칸 오프셋을 실제 보드 높이에 반영
        float hexOffsetHeight = 0f;
        if (_cachedBoardShape == BoardShape.Hexagon && _board.Width > 1)
        {
            hexOffsetHeight = cellSize * 0.5f;
        }

        float boardHeight = (_board.Height * cellSize) + hexOffsetHeight;
        float totalRequiredWidth = (_board.Width * cellSize) + (padding * 2f);
        float totalRequiredHeight = boardHeight + (padding * 2f);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenAspect = (float)Screen.width / Screen.height;
            float sizeByHeight = totalRequiredHeight / 2f;
            float sizeByWidth = (totalRequiredWidth / 2f) / screenAspect;

            mainCamera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
            mainCamera.transform.position = new Vector3(0, 0, -10f);
        }

        float finalY = 0f;
        if (mainCamera != null)
        {
            float camHeightHalf = mainCamera.orthographicSize;
            float boardHeightHalf = boardHeight / 2f;
            float availableSpace = camHeightHalf - boardHeightHalf - padding;
            finalY = offsetY * 2f * availableSpace;
        }

        transform.localPosition = new Vector3(0, finalY, 0);
    }

    /// <summary><paramref name="camera"/>에서 보드 전체와 테두리가 <paramref name="area"/> 화면 픽셀 영역 안에 들어오도록
    /// 배치합니다. 상대 보드는 <paramref name="maxScale"/>까지 확대합니다.</summary>
    internal void FitToScreen(Camera camera, Rect area, float maxScale)
    {
        if (_board == null || camera == null || camera.pixelHeight <= 0 || area.width <= 0f || area.height <= 0f)
        {
            return;
        }
        float stagger = _cachedBoardShape == BoardShape.Hexagon ? cellSize * 0.5f : 0f;
        float height = _board.Height * cellSize;
        float centerY = -stagger;
        if (_board.Width > 1)
        {
            height += stagger;
            centerY *= 0.5f;
        }
        float worldPerPixel = 2f * camera.orthographicSize / camera.pixelHeight;
        float fitX = area.width * worldPerPixel / (_board.Width * cellSize + 2f * padding);
        float fitY = area.height * worldPerPixel / (height + 2f * padding);
        float scale = Mathf.Min(maxScale, Mathf.Min(fitX, fitY));
        float depth = camera.WorldToScreenPoint(transform.position).z;
        Vector3 center = camera.ScreenToWorldPoint(new Vector3(area.center.x, area.center.y, depth));
        float previousWorldCellSize = WorldCellSize;
        transform.localScale = Vector3.one * scale;
        transform.position = center - transform.TransformVector(Vector3.up * centerY);
        RefreshLineWidths(previousWorldCellSize);
    }

    /// <summary> 기존 셀과 블럭 뷰를 반환하고 보드 표시 상태를 비웁니다. </summary>
    private void ClearBoard()
    {
        StopAllCoroutines();

        foreach (PuzzleCellView view in _cellViews.Values)
        {
            if (view != null)
            {
                PoolManager.Instance.Release(view.gameObject);
            }
        }

        foreach (PuzzleBlockView view in _blockViews.Values)
        {
            if (view != null)
            {
                PoolManager.Instance.Release(view.gameObject);
            }
        }

        _cellViews.Clear();
        _blockViews.Clear();

        while (_actionQueue.Count > 0)
        {
            ReleaseActionGroupList(_actionQueue.Dequeue());
        }

        _isAnimating = false;
        _previousLinkPath.Clear();
        HideAllLinkPointMarkers();
        ClearFeedback();

        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 0;
        }
    }

    /// <summary> 지정 좌표에 셀 뷰를 생성하고 초기화합니다. </summary>
    /// <param name="gridPos">뷰를 배치할 격자 좌표</param>
    /// <param name="cellData">표시할 셀 데이터</param>
    private void CreateCellView(GridPos gridPos, PuzzleCell cellData)
    {
        if (_cellPrefabObj == null)
        {
            return;
        }

        GameObject cellObj = PoolManager.Instance.Get(_cellPrefabObj, cellRoot);
        cellObj.transform.localPosition = GetLocalPos(gridPos);
        cellObj.name = $"Cell_{gridPos.X}_{gridPos.Y}";

        PuzzleCellView cellView = cellObj.GetComponent<PuzzleCellView>();
        if (cellView == null)
        {
            cellView = cellObj.AddComponent<PuzzleCellView>();
        }

        cellView.Initialize(cellData, gridPos, this, _cachedBoardShape);
        _cellViews[gridPos] = cellView;
    }

    /// <summary> 지정 좌표에 블럭 뷰를 생성하고 초기화합니다. </summary>
    /// <param name="gridPos">뷰를 배치할 격자 좌표</param>
    /// <param name="blockData">표시할 블럭 데이터</param>
    private void CreateBlockView(GridPos gridPos, Block blockData)
    {
        if (_blockPrefabObj == null)
        {
            return;
        }

        GameObject blockObj = PoolManager.Instance.Get(_blockPrefabObj, blockRoot);
        blockObj.transform.localPosition = GetLocalPos(gridPos);
        blockObj.name = $"Block_{gridPos.X}_{gridPos.Y}";

        PuzzleBlockView blockView = blockObj.GetComponent<PuzzleBlockView>();
        if (blockView != null)
        {
            blockView.Initialize(blockData, gridPos, this);
            _blockViews[gridPos] = blockView;
        }
    }

    /// <summary> 블럭 상태와 링크 경로를 갱신하고 모델의 연출 액션을 수집합니다. </summary>
    private void Update()
    {
        if (_board == null)
        {
            return;
        }

        UpdateLineRenderer();
        if (_pivotActive)
        {
            _pivotTravel = Mathf.Repeat(_pivotTravel + Time.deltaTime * 1.6f, 4f);
            for (int i = 0; i < _pivotContourCount; i++)
            {
                UpdatePivotTrails(_pivotContours[i]);
            }
        }

        foreach (KeyValuePair<GridPos, PuzzleBlockView> kvp in _blockViews)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdateStateVisual();
            }
        }

        List<BoardViewAction> actions = _board.FetchActions();
        if (actions != null && actions.Count > 0)
        {
            // LINQ 대신 수동 그룹화로 GC 할당 방지
            GroupActionsByFrameAndOrder(actions);
        }

        if (!_isAnimating && _actionQueue.Count > 0)
        {
            StartCoroutine(ProcessActionQueue());
        }
    }

    /// <summary> 링크의 전체 좌표를 비교하고 변경된 경로를 별도 버퍼에 복사합니다. </summary>
    /// <param name="path">현재 모델의 읽기 전용 링크 경로</param>
    /// <returns>화면에 반영할 경로가 변경되었으면 참</returns>
    private bool CacheLinkPathIfChanged(IReadOnlyList<GridPos> path)
    {
        bool changed = path.Count != _previousLinkPath.Count;
        for (int i = 0; !changed && i < path.Count; i++)
        {
            if (path[i] != _previousLinkPath[i])
            {
                changed = true;
            }
        }
        if (!changed)
        {
            return false;
        }

        _previousLinkPath.Clear();
        for (int i = 0; i < path.Count; i++)
        {
            _previousLinkPath.Add(path[i]);
        }
        return true;
    }

    /// <summary> 변경된 링크 경로를 선과 포인트 마커에 반영합니다. </summary>
    private void UpdateLineRenderer()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        if (_board is LinkPuzzleBoard linkBoard)
        {
            IReadOnlyList<GridPos> path = linkBoard.GetCurrentLinkPath();
            if (path != null && path.Count > 0)
            {
                bool wasReady = _previousLinkPath.Count >= LinkPuzzleBoard.MinimumLinkLength;
                bool hasNewSegment = HasNewTipSegment(_previousLinkPath, path);
                // 길이와 끝점이 같아도 중간 경로가 달라졌으면 다시 그립니다.
                if (!CacheLinkPathIfChanged(path))
                {
                    return;
                }

                // LineRenderer는 최소 2개 포인트가 필요하므로,
                // 포인트가 1개일 때는 같은 위치에 2개를 찍어 점으로 표시
                int pointCount = Mathf.Max(path.Count, 2);
                _lineRenderer.positionCount = pointCount;

                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 localPos = GetLocalPos(path[i]);
                    localPos.z = -1f;
                    _lineRenderer.SetPosition(i, localPos);
                }

                // 포인트가 1개인 경우 두 번째 포인트를 동일 위치에 배치
                if (path.Count == 1)
                {
                    _lineRenderer.SetPosition(1, _lineRenderer.GetPosition(0));
                }

                // 각 연결 지점에 포인트 마커 표시
                UpdateLinkPointMarkers(path);
                bool isReady = path.Count >= LinkPuzzleBoard.MinimumLinkLength;
                _lineRenderer.startColor = isReady ? _readyTrailColor : _linkTrailColor;
                AnimateLinkTip(path, hasNewSegment);
                SetLinkWidth(isReady);
                if (!wasReady && isReady)
                {
                    PlayLinkRing(path[LinkPuzzleBoard.MinimumLinkLength - 1]);
                }
                else if (!isReady)
                {
                    HideLinkRing();
                }
            }
            else
            {
                if (_previousLinkPath.Count != 0)
                {
                    _previousLinkPath.Clear();
                    _lineRenderer.positionCount = 0;
                    HideAllLinkPointMarkers();
                    HideLinkRing();
                }
            }
        }
        else
        {
            if (_previousLinkPath.Count != 0)
            {
                _previousLinkPath.Clear();
                _lineRenderer.positionCount = 0;
                HideAllLinkPointMarkers();
                HideLinkRing();
            }
        }
    }

    /// <summary><paramref name="isReady"/>에 맞는 연결선의 굵기를 현재 보드의 월드 배율로 그립니다.</summary>
    private void SetLinkWidth(bool isReady)
    {
        float width = WorldCellSize * (isReady ? 0.15f : 0.1f);
        _lineRenderer.startWidth = width;
        _lineRenderer.endWidth = width;
    }

    /// <summary> 링크가 처음 성립한 <paramref name="pos"/>에서 원을 한 번 펼칩니다.</summary>
    private void PlayLinkRing(GridPos pos)
    {
        if (_linkRing == null)
        {
            _linkRing = CreateRing("LinkReadyRing", 102, cellSize * 0.48f);
        }
        HideLinkRing();
        _linkRing.gameObject.SetActive(true);
        _linkRing.transform.localPosition = GetLocalPos(pos);
        _linkRing.transform.localScale = Vector3.one * 0.3f;
        _linkRing.widthMultiplier = WorldCellSize * LinkRingWidth;
        _linkRing.transform.DOScale(1f, 0.18f).SetEase(Ease.OutQuad)
            .OnComplete(() => _linkRing.gameObject.SetActive(false));
    }

    /// <summary> 진행 중인 링크 성립 표시를 즉시 숨깁니다.</summary>
    private void HideLinkRing()
    {
        if (_linkRing != null)
        {
            _linkRing.transform.DOKill();
            _linkRing.gameObject.SetActive(false);
        }
    }

    /// <summary>보드 아래에 <paramref name="objectName"/> 폐곡선을 만들고 <paramref name="sortingOrder"/>로 정렬합니다.</summary>
    private LineRenderer CreateOutline(string objectName, int sortingOrder)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(transform, false);
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = _lineMaterial;
        line.useWorldSpace = false;
        line.loop = true;
        line.sortingLayerName = "Ingame";
        line.sortingOrder = sortingOrder;
        return line;
    }

    /// <summary><paramref name="objectName"/>의 원을 <paramref name="radius"/> 반경으로 만들고 <paramref name="sortingOrder"/>로 정렬합니다.</summary>
    private LineRenderer CreateRing(string objectName, int sortingOrder, float radius)
    {
        LineRenderer ring = CreateOutline(objectName, sortingOrder);
        ring.positionCount = 32;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = 2f * Mathf.PI * i / ring.positionCount;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        return ring;
    }

    /// <summary><paramref name="active"/>가 켜지면 모서리에서 별빛을 터뜨리고 두 빛줄기를 테두리에 따라 이동시킵니다.</summary>
    public void SetPivotActive(bool active)
    {
        if (_pivotActive == active || !isActiveAndEnabled || _board == null || _lineMaterial == null)
        {
            return;
        }
        _pivotActive = active;
        if (active)
        {
            if (_pivotContourCount == 0)
            {
                BuildPivotPaths();
            }
            _pivotTravel = 0f;
        }
        for (int i = 0; i < _pivotContourCount; i++)
        {
            SetContourActive(_pivotContours[i], active);
        }
    }

    /// <summary><paramref name="contour"/>의 테두리와 빛줄기를 <paramref name="active"/> 상태로 전환합니다.</summary>
    private void SetContourActive(PivotContour contour, bool active)
    {
        LineRenderer border = contour.Border;
        border.DOKill();
        if (active)
        {
            border.gameObject.SetActive(true);
            SetOutlineAlpha(border, 1f);
            PlayPivotIgnition(contour);
            UpdatePivotTrails(contour);
            EmitPivotCornerStars(contour);
        }
        foreach (LineRenderer trail in contour.Trails)
        {
            trail.gameObject.SetActive(active);
        }

        float opacity = active ? 0.68f : 0f;
        Tween fade = DOTween.To(() => border.startColor.a, alpha => SetOutlineAlpha(border, alpha), opacity, 0.28f).SetTarget(border);
        if (!active)
        {
            contour.Ignition.DOKill();
            contour.Ignition.gameObject.SetActive(false);
            fade.OnComplete(() => border.gameObject.SetActive(false));
        }
    }

    /// <summary><paramref name="contour"/>의 네 모서리에 가장 가까운 테두리 꼭짓점에서 별빛을 방출합니다.</summary>
    private void EmitPivotCornerStars(PivotContour contour)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 inward = new Vector3(i == 0 || i == 3 ? 1f : -1f, i < 2 ? 1f : -1f);
            Vector3 target = contour.LocalBounds.center - Vector3.Scale(inward, contour.LocalBounds.extents);
            Vector3 corner = contour.Points[0];
            foreach (Vector3 point in contour.Points)
            {
                if ((point - target).sqrMagnitude < (corner - target).sqrMagnitude)
                {
                    corner = point;
                }
            }
            Vector3 origin = corner + inward * (cellSize * 0.19f);
            EmitPivotStar(origin, inward * 0.38f, 0.74f, _pivotCore);
            EmitPivotStar(origin, new Vector3(inward.x * 1.6f, inward.y * 0.24f), 0.38f, _pivotGold);
            EmitPivotStar(origin, new Vector3(inward.x * 0.24f, inward.y * 1.6f), 0.42f, _pivotAqua);
        }
    }

    /// <summary>새 보드를 처음 발동할 때만 외곽점과 발동 띠의 좌표를 준비합니다.</summary>
    private void BuildPivotPaths()
    {
        if (_cachedBoardShape == BoardShape.Hexagon)
        {
            BuildHexPivotPaths();
        }
        else
        {
            PivotContour contour = GetNextPivotContour();
            Vector3 bottom = GetLocalPos(new GridPos(0, 0)) - new Vector3(1f, 1f) * (cellSize * 0.55f);
            contour.Points.Add(bottom);
            contour.Points.Add(new Vector3(-bottom.x, bottom.y));
            contour.Points.Add(new Vector3(-bottom.x, -bottom.y));
            contour.Points.Add(new Vector3(bottom.x, -bottom.y));
        }
        for (int i = 0; i < _pivotContourCount; i++)
        {
            PivotContour contour = _pivotContours[i];
            contour.LocalBounds = new Bounds(contour.Points[0], Vector3.zero);
            foreach (Vector3 point in contour.Points)
            {
                contour.LocalBounds.Encapsulate(point);
            }
            contour.Ignition.transform.localPosition = contour.LocalBounds.center;
            contour.Border.positionCount = contour.Points.Count;
            contour.Ignition.positionCount = contour.Points.Count;
            for (int vertex = 0; vertex < contour.Points.Count; vertex++)
            {
                Vector3 point = contour.Points[vertex];
                contour.Border.SetPosition(vertex, point);
                contour.Ignition.SetPosition(vertex, point - contour.LocalBounds.center);
            }
            SetPivotWidths(contour, WorldCellSize);
        }
    }

    /// <summary>재사용할 테두리를 다음 보드 영역에 할당하며 부족한 만큼만 렌더러를 만듭니다.</summary>
    private PivotContour GetNextPivotContour()
    {
        if (_pivotContourCount == _pivotContours.Count)
        {
            PivotContour created = new PivotContour();
            created.Border = CreateOutline("PivotBorder", 22);
            created.Border.numCornerVertices = 5;
            created.Ignition = CreateOutline("PivotIgnition", 23);
            created.Ignition.numCornerVertices = 5;
            for (int i = 0; i < created.Trails.Length; i++)
            {
                LineRenderer trail = CreateOutline("PivotTrail", 24);
                trail.loop = false;
                trail.numCornerVertices = 5;
                trail.numCapVertices = 3;
                Color tail = i == 0 ? _pivotGold : _pivotAqua;
                trail.startColor = new Color(tail.r, tail.g, tail.b, 0.18f);
                trail.endColor = i == 0 ? _pivotCore : _pivotAqua;
                trail.widthCurve = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);
                created.Trails[i] = trail;
            }
            _pivotContours.Add(created);
        }
        PivotContour contour = _pivotContours[_pivotContourCount++];
        contour.Points.Clear();
        return contour;
    }

    /// <summary>왼쪽 아래부터 각 육각 타일 덩어리를 한 번씩 방문해 서로 떨어진 영역의 테두리를 나눕니다.</summary>
    private void BuildHexPivotPaths()
    {
        HashSet<GridPos> remaining = new HashSet<GridPos>();
        foreach (KeyValuePair<GridPos, PuzzleCell> entry in _board.Cells)
        {
            if (entry.Value.CellType != CellType.Close)
            {
                remaining.Add(entry.Key);
            }
        }
        Stack<GridPos> pending = new Stack<GridPos>();
        for (int x = 0; x < _board.Width && remaining.Count > 0; x++)
        {
            for (int y = 0; y < _board.Height && remaining.Count > 0; y++)
            {
                GridPos first = new GridPos(x, y);
                if (!remaining.Remove(first))
                {
                    continue;
                }
                pending.Push(first);
                while (pending.Count > 0)
                {
                    GridPos cell = pending.Pop();
                    for (int edge = 0; edge < 6; edge++)
                    {
                        GridPos adjacent = GetHexNeighbor(cell, edge);
                        if (remaining.Remove(adjacent))
                        {
                            pending.Push(adjacent);
                        }
                    }
                }
                TraceHexPivotPerimeter(first, GetNextPivotContour().Points);
            }
        }
    }

    /// <summary><paramref name="cell"/>의 <paramref name="edge"/> 변을 공유하는 이웃 좌표를 짝수 열 배치에 맞춰 반환합니다.</summary>
    private static GridPos GetHexNeighbor(GridPos cell, int edge)
    {
        GridPos offset = _evenHexNeighbors[edge];
        int rowShift = offset.X == 0 ? 0 : (cell.X & 1);
        return new GridPos(cell.X + offset.X, cell.Y + offset.Y + rowShift);
    }

    /// <summary>덩어리의 맨 왼쪽 아래 칸 <paramref name="first"/>에서 외곽을 한 바퀴 돌며 <paramref name="points"/>를 채웁니다.</summary>
    private void TraceHexPivotPerimeter(GridPos first, List<Vector3> points)
    {
        GridPos cell = first;
        int edge = 0;
        do
        {
            points.Add(GetLocalPos(cell) + (Vector3)(_hexCorners[edge] * cellSize));
            edge = (edge + 1) % 6;
            GridPos adjacent = GetHexNeighbor(cell, edge);
            PuzzleCell neighbor = _board.GetCell(adjacent);
            if (neighbor != null && neighbor.CellType != CellType.Close)
            {
                // 다음 변을 이웃과 공유하면 같은 꼭짓점에서 이웃의 다음 노출 변으로 이어집니다.
                cell = adjacent;
                edge = (edge + 4) % 6;
            }
        }
        while (cell != first || edge != 0);
    }

    /// <summary><paramref name="contour"/>의 윤곽을 따라 발동 빛을 펼칩니다. 육각 보드에서는 타일 테두리 폭 안에서 빛납니다.</summary>
    private void PlayPivotIgnition(PivotContour contour)
    {
        Vector3 half = contour.LocalBounds.extents;
        LineRenderer ignition = contour.Ignition;
        ignition.DOKill();
        float insetRatio = _cachedBoardShape == BoardShape.Hexagon ? PivotBorderWidth : 0.65f;
        float inset = Mathf.Min(insetRatio * cellSize, Mathf.Min(half.x, half.y) * 0.5f);
        ignition.transform.localScale = new Vector3(1f - inset / half.x, 1f - inset / half.y, 1f);
        ignition.startColor = ignition.endColor = _pivotCore;
        ignition.gameObject.SetActive(true);
        ignition.transform.DOScale(Vector3.one, 0.44f).SetEase(Ease.OutQuad).SetTarget(ignition);
        DOTween.To(() => ignition.startColor.a, alpha => SetOutlineAlpha(ignition, alpha), 0f, 0.44f)
            .SetEase(Ease.InQuad).SetTarget(ignition).OnComplete(() => ignition.gameObject.SetActive(false));
    }

    /// <summary><paramref name="contour"/>의 반대편에서 출발한 빛줄기 두 개를 그 영역의 바깥 변을 따라 이동시킵니다.</summary>
    private void UpdatePivotTrails(PivotContour contour)
    {
        List<Vector3> points = contour.Points;
        int count = points.Count;
        for (int i = 0; i < contour.Trails.Length; i++)
        {
            LineRenderer trail = contour.Trails[i];
            float head = (_pivotTravel + i * 2f) * (count / 4f);
            float tail = head - count * 0.2f;
            int firstCorner = Mathf.FloorToInt(tail) + 1;
            int endCorner = Mathf.CeilToInt(head);
            trail.positionCount = 2 + endCorner - firstCorner;
            trail.SetPosition(0, GetPivotBorderPoint(points, tail));
            for (int corner = firstCorner; corner < endCorner; corner++)
            {
                trail.SetPosition(1 + corner - firstCorner, points[(corner + count) % count]);
            }
            trail.SetPosition(trail.positionCount - 1, GetPivotBorderPoint(points, head));
        }
    }

    /// <summary><paramref name="points"/>의 모서리 간격으로 나타낸 <paramref name="distance"/>를 테두리 위 로컬 좌표로 변환합니다.</summary>
    private static Vector3 GetPivotBorderPoint(List<Vector3> points, float distance)
    {
        int side = Mathf.FloorToInt(distance);
        int count = points.Count;
        int vertex = (side + count) % count;
        return Vector3.Lerp(points[vertex], points[(vertex + 1) % count], distance - side);
    }

    /// <summary><paramref name="contour"/>의 모든 피벗 선 굵기를 <paramref name="worldCellSize"/>에 맞춥니다.</summary>
    private static void SetPivotWidths(PivotContour contour, float worldCellSize)
    {
        contour.Border.widthMultiplier = worldCellSize * PivotBorderWidth;
        contour.Ignition.widthMultiplier = worldCellSize * 0.09f;
        foreach (LineRenderer trail in contour.Trails)
        {
            trail.widthMultiplier = worldCellSize * PivotTrailWidth;
        }
    }

    /// <summary>링크와 피벗 선을 현재 칸 크기에 맞추고 <paramref name="previousWorldCellSize"/> 대비 배율로 재생 중인 폭탄 충격파의 굵기도 조정합니다.</summary>
    private void RefreshLineWidths(float previousWorldCellSize)
    {
        if (_lineRenderer != null && _previousLinkPath.Count > 0)
        {
            SetLinkWidth(_previousLinkPath.Count >= LinkPuzzleBoard.MinimumLinkLength);
        }
        float worldCellSize = WorldCellSize;
        if (_linkRing != null)
        {
            _linkRing.widthMultiplier = worldCellSize * LinkRingWidth;
        }
        if (previousWorldCellSize > 0f)
        {
            // 충격파마다 폭탄 반경이 다르므로 현재 굵기에 보드의 배율 변화만 반영합니다.
            float scaleRatio = worldCellSize / previousWorldCellSize;
            foreach (LineRenderer wave in _bombWaves)
            {
                if (wave.gameObject.activeSelf)
                {
                    wave.widthMultiplier *= scaleRatio;
                }
            }
        }
        for (int i = 0; i < _pivotContourCount; i++)
        {
            SetPivotWidths(_pivotContours[i], worldCellSize);
        }
        foreach (LineRenderer outline in _pivotOutlines)
        {
            outline.widthMultiplier = worldCellSize * PivotNeighborWidth;
        }
    }

    /// <summary><paramref name="outline"/>의 금빛을 <paramref name="alpha"/> 세기로 표시합니다.</summary>
    private static void SetOutlineAlpha(LineRenderer outline, float alpha)
    {
        Color tint = _pivotGold;
        tint.a = alpha;
        outline.startColor = tint;
        outline.endColor = tint;
    }

    /// <summary><paramref name="outline"/>의 꼭짓점을 로컬 <paramref name="rect"/>의 네 모서리에 맞춥니다.</summary>
    private static void SetRectangle(LineRenderer outline, Rect rect)
    {
        outline.positionCount = 4;
        outline.SetPosition(0, new Vector3(rect.xMin, rect.yMin));
        outline.SetPosition(1, new Vector3(rect.xMax, rect.yMin));
        outline.SetPosition(2, new Vector3(rect.xMax, rect.yMax));
        outline.SetPosition(3, new Vector3(rect.xMin, rect.yMax));
    }

    /// <summary>일반 파괴와 동시에 피벗이 추가한 <paramref name="position"/>의 타일을 짧게 강조합니다.</summary>
    private void PlayPivotOutline(GridPos position)
    {
        LineRenderer outline = GetPivotOutline();
        Vector3 origin = GetLocalPos(position);
        outline.transform.localPosition = origin;
        float half = cellSize * 0.46f;
        if (_cachedBoardShape == BoardShape.Hexagon)
        {
            outline.positionCount = 6;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * half, Mathf.Sin(angle) * half));
            }
        }
        else
        {
            SetRectangle(outline, Rect.MinMaxRect(-half, -half, half, half));
        }
        outline.widthMultiplier = WorldCellSize * PivotNeighborWidth;
        outline.gameObject.SetActive(true);
        outline.transform.localScale = Vector3.one * 0.70f;
        outline.startColor = outline.endColor = _pivotCore;
        /// <summary><paramref name="progress"/>에 맞춰 피벗 칸의 윤곽을 넓히며 금빛을 흐리게 합니다.</summary>
        void UpdateOutline(float progress)
        {
            float remaining = 1f - progress;
            outline.transform.localScale = Vector3.one * Mathf.Lerp(1.06f, 0.70f, remaining * remaining);
            Color color = Color.Lerp(_pivotCore, _pivotGold, progress);
            color.a = 1f - progress * progress;
            outline.startColor = outline.endColor = color;
        }
        DOVirtual.Float(0f, 1f, 0.29f, UpdateOutline).SetEase(Ease.Linear).SetTarget(outline).OnComplete(() => outline.gameObject.SetActive(false));
        float side = ((position.X + position.Y) & 1) == 0 ? 1f : -1f;
        EmitPivotStar(origin, new Vector3(side * 0.72f, 0.84f), 0.46f, _pivotCore);
        EmitPivotStar(origin, new Vector3(-side * 0.65f, 0.34f), 0.27f, _pivotAqua);
    }

    /// <summary>로컬 <paramref name="origin"/>에서 초당 셀 단위 <paramref name="velocity"/>로, 셀 대비 <paramref name="size"/> 크기의 <paramref name="color"/> 별을 방출합니다.</summary>
    private void EmitPivotStar(Vector3 origin, Vector3 velocity, float size, Color color)
    {
        if (_pivotSparks == null)
        {
            CreatePivotSparks();
        }
        if (!_pivotSparks.isPlaying)
        {
            _pivotSparks.Play(false);
        }
        ParticleSystem.EmitParams spark = new ParticleSystem.EmitParams
        {
            position = origin,
            velocity = velocity * cellSize,
            startSize = size * cellSize,
            startLifetime = 0.30f + size * 0.30f,
            startColor = color,
            rotation = velocity.x * 15f
        };
        _pivotSparks.Emit(spark, 1);
    }

    /// <summary>공유 별 메시를 쓰는 로컬 입자 시스템 하나를 준비해 보드당 최대 48개의 반짝임을 그립니다.</summary>
    private void CreatePivotSparks()
    {
        GameObject obj = new GameObject("PivotSparks");
        obj.transform.SetParent(transform, false);
        _pivotSparks = obj.AddComponent<ParticleSystem>();
        _pivotSparks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = _pivotSparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ParticleSystem.EmissionModule emission = _pivotSparks.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = _pivotSparks.shape;
        shape.enabled = false;
        ParticleSystem.SizeOverLifetimeModule scale = _pivotSparks.sizeOverLifetime;
        scale.enabled = true;
        scale.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        // 흰 중심에서 네 개의 긴 꼭짓점으로 색이 번지는 별을 한 메시로 그립니다.
        Vector3[] vertices = new Vector3[9];
        Color32[] colors = new Color32[9];
        int[] triangles = new int[24];
        colors[0] = Color.white;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            float radius = (i & 1) == 0 ? 0.5f : 0.12f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            colors[i + 1] = new Color32(255, 255, 255, 195);
            triangles[i * 3 + 1] = (i + 1) % 8 + 1;
            triangles[i * 3 + 2] = i + 1;
        }
        _pivotStarMesh = new Mesh { name = "PivotStar", vertices = vertices, colors32 = colors, triangles = triangles };
        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = _pivotStarMesh;
        renderer.sharedMaterial = _lineMaterial;
        renderer.sortingLayerName = "Ingame";
        renderer.sortingOrder = 25;
    }

    /// <summary>재사용 가능한 피벗 셀 테두리를 반환하고 모두 재생 중이면 하나 더 만듭니다.</summary>
    private LineRenderer GetPivotOutline()
    {
        foreach (LineRenderer outline in _pivotOutlines)
        {
            if (!outline.gameObject.activeSelf)
            {
                return outline;
            }
        }
        LineRenderer created = CreateOutline("PivotNeighbor", 23);
        created.numCornerVertices = 3;
        _pivotOutlines.Add(created);
        return created;
    }

    /// <summary> 파괴 액션에 담긴 폭탄마다 해당 좌표에서 판정 반경까지 충격파를 펼칩니다. </summary>
    private void PlayBombWaves(BoardViewAction action)
    {
        if (action.blockData == null)
        {
            return;
        }
        foreach (IGimmick gimmick in action.blockData.Gimmicks)
        {
            if (gimmick is BombGimmick bomb)
            {
                LineRenderer wave = GetBombWave();
                wave.gameObject.SetActive(true);
                wave.transform.localPosition = GetLocalPos(action.position);
                wave.transform.localScale = Vector3.one * 0.12f;
                wave.widthMultiplier = WorldCellSize * 0.045f / bomb.Radius;
                wave.transform.DOScale((float)bomb.Radius, 0.24f).SetEase(Ease.OutQuad)
                    .OnComplete(() => wave.gameObject.SetActive(false));
            }
        }
    }

    /// <summary> 쉬고 있는 충격파를 재사용하고, 연쇄 폭발로 모두 사용 중이면 하나를 추가합니다. </summary>
    private LineRenderer GetBombWave()
    {
        foreach (LineRenderer wave in _bombWaves)
        {
            if (!wave.gameObject.activeSelf)
            {
                return wave;
            }
        }
        LineRenderer created = CreateRing("BombShockwave", 21, cellSize);
        Color orange = new Color(1f, 0.62f, 0.18f, 0.85f);
        created.startColor = orange;
        created.endColor = orange;
        _bombWaves.Add(created);
        return created;
    }

    /// <summary> 파괴되는 <paramref name="block"/>의 스프라이트를 작은 파편 세 개로 흩뿌립니다.</summary>
    private void PlayMatchFragments(PuzzleBlockView block)
    {
        SpriteRenderer source = block.SpriteRenderer;
        if (source == null || source.sprite == null)
        {
            return;
        }
        Vector3 origin = transform.InverseTransformPoint(source.bounds.center);
        for (int i = 0; i < 3; i++)
        {
            SpriteRenderer fragment = GetFreeFragment();
            if (fragment == null)
            {
                return;
            }
            fragment.sprite = source.sprite;
            fragment.gameObject.SetActive(true);
            float angle = (90f + 120f * i) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            float scale = cellSize * 0.17f / source.sprite.bounds.size.x;
            fragment.transform.localPosition = origin;
            fragment.transform.localScale = Vector3.one * scale;
            fragment.transform.DOScale(0f, 0.19f).SetEase(Ease.InQuad);
            fragment.transform.DOLocalMove(origin + direction * cellSize * 0.48f, 0.19f)
                .SetEase(Ease.OutQuad).OnComplete(() => fragment.gameObject.SetActive(false));
        }
    }

    /// <summary> 보드의 동시 표시 상한 안에서 재사용할 파편을 가져옵니다.</summary>
    private SpriteRenderer GetFreeFragment()
    {
        foreach (SpriteRenderer fragment in _matchFragments)
        {
            if (!fragment.gameObject.activeSelf)
            {
                return fragment;
            }
        }
        if (_matchFragments.Count >= MaxMatchFragments)
        {
            return null;
        }
        GameObject obj = new GameObject("MatchFragment");
        obj.transform.SetParent(transform, false);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Ingame";
        renderer.sortingOrder = 20;
        _matchFragments.Add(renderer);
        return renderer;
    }

    /// <summary> 보드를 비우거나 종료할 때 파편과 링크 표시의 트윈을 정리합니다.</summary>
    private void ClearFeedback()
    {
        HideLinkRing();
        _pivotActive = false;
        for (int i = 0; i < _pivotContourCount; i++)
        {
            PivotContour contour = _pivotContours[i];
            contour.Border.DOKill();
            contour.Border.gameObject.SetActive(false);
            contour.Ignition.DOKill();
            contour.Ignition.gameObject.SetActive(false);
            foreach (LineRenderer trail in contour.Trails)
            {
                trail.gameObject.SetActive(false);
            }
        }
        if (_pivotSparks != null)
        {
            _pivotSparks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        foreach (LineRenderer outline in _pivotOutlines)
        {
            outline.DOKill();
            outline.gameObject.SetActive(false);
        }
        foreach (LineRenderer wave in _bombWaves)
        {
            wave.transform.DOKill();
            wave.gameObject.SetActive(false);
        }
        foreach (SpriteRenderer fragment in _matchFragments)
        {
            if (fragment != null)
            {
                fragment.transform.DOKill();
                fragment.sprite = null;
                fragment.gameObject.SetActive(false);
            }
        }
    }

    /// <summary> 현재 경로의 지점을 표시하고 이전 끝점의 이동과 크기를 초기화합니다. </summary>
    private void UpdateLinkPointMarkers(IReadOnlyList<GridPos> path)
    {
        StopLinkTipAnimation();
        // 필요한 만큼 마커 오브젝트 확보
        while (_linkPointMarkers.Count < path.Count)
        {
            GameObject marker = new GameObject("LinkPoint");
            marker.transform.SetParent(transform, false);

            SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
            sr.sprite = _circleSprite;
            sr.sortingLayerName = "Ingame";
            sr.sortingOrder = 101;
            sr.color = Color.white;

            marker.SetActive(false);
            _linkPointMarkers.Add(marker);
        }

        // 경로에 맞춰 마커 위치, 색상 지정 및 활성화
        for (int i = 0; i < path.Count; i++)
        {
            GameObject marker = _linkPointMarkers[i];
            Vector3 localPos = GetLocalPos(path[i]);
            localPos.z = -2f;
            marker.transform.localPosition = localPos;
            marker.transform.localScale = Vector3.one * GetLinkMarkerScale(0.19f);

            marker.SetActive(true);
        }

        // 남는 마커는 비활성화
        for (int i = path.Count; i < _linkPointMarkers.Count; i++)
        {
            _linkPointMarkers[i].SetActive(false);
        }
    }

    /// <summary> 셀 너비에 대한 마커 지름 비율을 스프라이트 배율로 바꿉니다. </summary>
    private float GetLinkMarkerScale(float diameterRatio) => cellSize * diameterRatio / _circleSprite.bounds.size.x;

    /// <summary> 이전 경로와 현재 경로의 마지막 선분을 비교해 같은 프레임에 되짚고 새로 이은 경우도 찾습니다. </summary>
    private static bool HasNewTipSegment(IReadOnlyList<GridPos> previous, IReadOnlyList<GridPos> current)
    {
        int last = current.Count - 1;
        if (last < 1)
        {
            return false;
        }
        if (last >= previous.Count)
        {
            return true;
        }
        return current[last] != previous[last] || current[last - 1] != previous[last - 1];
    }

    /// <summary> 경로의 끝점을 강조하고 새 선분이 생겼을 때 그 구간을 따라 빛이 이동하게 합니다. </summary>
    private void AnimateLinkTip(IReadOnlyList<GridPos> path, bool hasNewSegment)
    {
        int last = path.Count - 1;
        Transform tip = _linkPointMarkers[last].transform;
        float restingScale = GetLinkMarkerScale(0.31f);
        tip.localScale = Vector3.one * restingScale;
        if (!hasNewSegment)
        {
            return;
        }

        Vector3 destination = tip.localPosition;
        tip.localPosition = _linkPointMarkers[last - 1].transform.localPosition;
        UpdateTipLine();
        _linkTipTravel = tip.DOLocalMove(destination, 0.07f).SetRecyclable(false).SetEase(Ease.OutQuad).OnUpdate(UpdateTipLine);
        tip.localScale = Vector3.one * GetLinkMarkerScale(0.43f);
        _linkTipPulse = tip.DOScale(restingScale, 0.14f).SetRecyclable(false).SetEase(Ease.OutQuad);

        /// <summary> 마지막 선분이 이동 중인 끝점 마커에 닿도록 갱신합니다. </summary>
        void UpdateTipLine()
        {
            Vector3 point = tip.localPosition;
            point.z = -1f;
            _lineRenderer.SetPosition(last, point);
        }
    }

    /// <summary> 드래그 경로가 바뀌거나 닫힐 때 이전 끝점 연출을 멈춥니다. </summary>
    private void StopLinkTipAnimation()
    {
        if (_linkTipTravel != null)
        {
            _linkTipTravel.Kill();
            _linkTipTravel = null;
        }
        if (_linkTipPulse != null)
        {
            _linkTipPulse.Kill();
            _linkTipPulse = null;
        }
    }

    /// <summary> 드래그 끝점 연출을 중단하고 모든 포인트 마커를 숨깁니다. </summary>
    private void HideAllLinkPointMarkers()
    {
        StopLinkTipAnimation();
        for (int i = 0; i < _linkPointMarkers.Count; i++)
        {
            _linkPointMarkers[i].SetActive(false);
        }
    }

    /// <summary>
    /// 뷰 액션 리스트를 (frame, orderIndex) 기준으로 그룹화하여 큐에 넣습니다.
    /// LINQ GroupBy/OrderBy 대신 수동 정렬/그룹화로 GC 할당을 방지합니다.
    /// </summary>
    /// <param name="actions">그룹화할 뷰 액션 리스트 (정렬 완료 상태)</param>
    private void GroupActionsByFrameAndOrder(List<BoardViewAction> actions)
    {
        // FetchActions()에서 이미 frame → orderIndex 순으로 정렬되어 반환됨
        List<BoardViewAction> currentGroup = GetActionGroupList();
        uint prevFrame = actions[0].frame;
        uint prevOrder = actions[0].orderIndex;

        for (int i = 0; i < actions.Count; i++)
        {
            BoardViewAction action = actions[i];
            if (action.frame != prevFrame || action.orderIndex != prevOrder)
            {
                _actionQueue.Enqueue(currentGroup);
                currentGroup = GetActionGroupList();
                prevFrame = action.frame;
                prevOrder = action.orderIndex;
            }
            currentGroup.Add(action);
        }

        if (currentGroup.Count > 0)
        {
            _actionQueue.Enqueue(currentGroup);
        }
    }

    /// <summary>
    /// 액션 그룹 리스트를 풀에서 가져옵니다.
    /// </summary>
    private List<BoardViewAction> GetActionGroupList()
    {
        if (_actionGroupPool.Count > 0)
        {
            return _actionGroupPool.Pop();
        }

        return new List<BoardViewAction>();
    }

    /// <summary>
    /// 사용이 끝난 액션 그룹 리스트를 비우고 재사용 풀에 반환합니다.
    /// </summary>
    /// <param name="group">반환할 액션 그룹 리스트</param>
    private void ReleaseActionGroupList(List<BoardViewAction> group)
    {
        if (group == null)
        {
            return;
        }

        group.Clear();
        _actionGroupPool.Push(group);
    }

    /// <summary> ProcessActionQueue 내부에서 이동 액션을 재사용하는 임시 리스트 </summary>
    private readonly List<BoardViewAction> _tempMovementActions = new List<BoardViewAction>();

    /// <summary> ProcessActionQueue 내부에서 기타 액션을 재사용하는 임시 리스트 </summary>
    private readonly List<BoardViewAction> _tempOtherActions = new List<BoardViewAction>();

    /// <summary> 연출 액션 그룹을 순서대로 재생하고 완료를 기다립니다. </summary>
    private IEnumerator ProcessActionQueue()
    {
        _isAnimating = true;

        while (_actionQueue.Count > 0)
        {
            List<BoardViewAction> actionGroup = _actionQueue.Dequeue();

            // LINQ 대신 수동 분류로 GC 할당 방지
            _tempMovementActions.Clear();
            _tempOtherActions.Clear();
            for (int i = 0; i < actionGroup.Count; i++)
            {
                BoardViewAction action = actionGroup[i];
                if (action.type == ViewType.Move || action.type == ViewType.Fall || action.type == ViewType.CreateAndFall)
                {
                    _tempMovementActions.Add(action);
                }
                else
                {
                    _tempOtherActions.Add(action);
                }
            }

            if (_tempMovementActions.Count > 0)
            {
                yield return StartCoroutine(ExecuteBatchMovement(_tempMovementActions));
            }

            if (_tempOtherActions.Count > 0)
            {
                int completedCount = 0;
                int totalCount = _tempOtherActions.Count;

                for (int i = 0; i < _tempOtherActions.Count; i++)
                {
                    ExecuteSingleAction(_tempOtherActions[i], () => completedCount++);
                }

                while (completedCount < totalCount)
                {
                    yield return null;
                }
            }

            ReleaseActionGroupList(actionGroup);
            yield return _actionGroupDelay;
        }

        _isAnimating = false;
    }

    /// <summary> ExecuteBatchMovement 내부에서 액션-뷰 매핑을 재사용하는 임시 리스트 (Dictionary 할당 방지) </summary>
    private readonly List<BoardViewAction> _batchActions = new List<BoardViewAction>();
    private readonly List<PuzzleBlockView> _batchViews = new List<PuzzleBlockView>();

    /// <summary> 기존 블럭 이동을 먼저 확보한 뒤 생성 낙하를 준비하고 함께 재생합니다. </summary>
    /// <param name="moveActions">함께 재생할 이동 및 낙하 액션 목록</param>
    private System.Collections.IEnumerator ExecuteBatchMovement(List<BoardViewAction> moveActions)
    {
        int completedCount = 0;
        int totalCount = moveActions.Count;

        _batchActions.Clear();
        _batchViews.Clear();

        // Move/Fall을 먼저 처리하여 기존 뷰를 _blockViews에서 제거한 뒤,
        // CreateAndFall을 처리해야 HandleImmediateDestroy가 이동 예정 블럭을 파괴하지 않음
        for (int i = 0; i < moveActions.Count; i++)
        {
            BoardViewAction action = moveActions[i];
            if (action.type == ViewType.Move || action.type == ViewType.Fall)
            {
                if (_blockViews.TryGetValue(action.position, out PuzzleBlockView view))
                {
                    _batchActions.Add(action);
                    _batchViews.Add(view);
                    _blockViews.Remove(action.position);
                }
            }
        }

        for (int i = 0; i < moveActions.Count; i++)
        {
            BoardViewAction action = moveActions[i];
            if (action.type == ViewType.CreateAndFall)
            {
                if (action.blockData != null && _blockPrefabObj != null)
                {
                    if (_blockViews.ContainsKey(action.targetPosition))
                    {
                        HandleImmediateDestroy(action.targetPosition);
                    }

                    GameObject blockObj = PoolManager.Instance.Get(_blockPrefabObj, blockRoot);
                    blockObj.transform.localPosition = GetLocalPos(action.position);
                    blockObj.name = $"Block_{action.targetPosition.X}_{action.targetPosition.Y}";

                    PuzzleBlockView bView = blockObj.GetComponent<PuzzleBlockView>();
                    if (bView != null)
                    {
                        bView.Initialize(action.blockData, action.position, this);
                        _batchActions.Add(action);
                        _batchViews.Add(bView);
                    }
                }
            }
        }

        for (int i = 0; i < _batchActions.Count; i++)
        {
            BoardViewAction action = _batchActions[i];
            PuzzleBlockView view = _batchViews[i];
            GridPos to = action.targetPosition;

            _blockViews[to] = view;

            Vector3 targetPos = GetLocalPos(to);
            System.Action onComplete = () =>
            {
                view.Initialize(view.GetBlockData(), to, this);
                completedCount++;
            };
            if (action.type == ViewType.Move)
            {
                view.PlayMoveAnimation(targetPos, onComplete);
            }
            else if (action.type == ViewType.CreateAndFall && action.position.Y >= _board.Height)
            {
                float topCellY = GetLocalPos(new GridPos(to.X, _board.Height - 1)).y;
                view.PlayFallAnimation(targetPos, onComplete, topCellY);
            }
            else
            {
                view.PlayFallAnimation(targetPos, onComplete);
            }
        }

        int processedCount = _batchActions.Count;
        if (processedCount < totalCount)
        {
            completedCount += (totalCount - processedCount);
        }

        while (completedCount < totalCount)
        {
            yield return null;
        }
    }

    /// <summary> 단일 연출 액션을 실행하고 완료 콜백을 전달합니다. </summary>
    /// <param name="action">실행할 연출 액션</param>
    /// <param name="onComplete">연출 완료 시 호출할 콜백</param>
    private void ExecuteSingleAction(BoardViewAction action, System.Action onComplete)
    {
        switch (action.type)
        {
            case ViewType.Destroy:
                if (action.IsPivotNeighbor)
                {
                    PlayPivotOutline(action.position);
                }
                PlayBombWaves(action);
                if (_blockViews.TryGetValue(action.position, out PuzzleBlockView dView))
                {
                    _blockViews.Remove(action.position);
                    /// <summary> 파괴 연출 후 뷰를 풀에 반환하고 완료를 알립니다. </summary>
                    void OnDestroyCompleted()
                    {
                        PoolManager.Instance.Release(dView.gameObject);
                        if (onComplete != null)
                        {
                            onComplete.Invoke();
                        }
                    }

                    PlayMatchFragments(dView);
                    dView.PlayDestroyAnimation(OnDestroyCompleted);
                }
                else
                {
                    if (onComplete != null)
                    {
                        onComplete.Invoke();
                    }
                }
                break;

            case ViewType.Create:
                if (action.blockData != null)
                {
                    if (_blockViews.ContainsKey(action.position))
                    {
                        HandleImmediateDestroy(action.position);
                    }

                    if (_blockPrefabObj != null)
                    {
                        GameObject blockObj = PoolManager.Instance.Get(_blockPrefabObj, blockRoot);
                        blockObj.transform.localPosition = GetLocalPos(action.position);
                        blockObj.name = $"Block_{action.position.X}_{action.position.Y}";

                        PuzzleBlockView bView = blockObj.GetComponent<PuzzleBlockView>();
                        if (bView != null)
                        {
                            bView.Initialize(action.blockData, action.position, this);
                            _blockViews[action.position] = bView;
                            bView.PlayCreateAnimation(onComplete);
                        }
                        else
                        {
                            if (onComplete != null)
                            {
                                onComplete.Invoke();
                            }
                        }
                    }
                    else
                    {
                        if (onComplete != null)
                        {
                            onComplete.Invoke();
                        }
                    }
                }
                else
                {
                    if (onComplete != null)
                    {
                        onComplete.Invoke();
                    }
                }
                break;

            default:
                if (onComplete != null)
                {
                    onComplete.Invoke();
                }
                break;
        }
    }

    /// <summary> 지정 좌표의 블럭 뷰를 즉시 제거하고 풀에 반환합니다. </summary>
    /// <param name="pos">대상 격자 좌표</param>
    private void HandleImmediateDestroy(GridPos pos)
    {
        if (_blockViews.TryGetValue(pos, out PuzzleBlockView view))
        {
            _blockViews.Remove(pos);
            if (view != null && view.gameObject != null)
            {
                view.transform.localScale = Vector3.one;
                PoolManager.Instance.Release(view.gameObject);
            }
        }
    }

    /// <summary> 사용자 입력이 허용된 보드에만 좌표를 전달합니다. </summary>
    /// <param name="pos">입력한 셀 좌표</param>
    public void OnBlockInput(GridPos pos)
    {
        if (InputEnabled && _board != null)
        {
            _board.Input(pos);
        }
    }

    /// <summary> 현재 모델의 블럭 배치에 맞춰 블럭 뷰를 다시 생성합니다. </summary>
    public void RefreshBlocks()
    {
        if (_board == null)
        {
            return;
        }

        foreach (PuzzleBlockView view in _blockViews.Values)
        {
            if (view != null && view.gameObject != null)
            {
                PoolManager.Instance.Release(view.gameObject);
            }
        }
        _blockViews.Clear();

        foreach (KeyValuePair<GridPos, PuzzleCell> kvp in _board.Cells)
        {
            GridPos gridPos = kvp.Key;
            PuzzleCell cell = kvp.Value;
            if (cell.Block != null)
            {
                CreateBlockView(gridPos, cell.Block);
            }
        }
    }
}
