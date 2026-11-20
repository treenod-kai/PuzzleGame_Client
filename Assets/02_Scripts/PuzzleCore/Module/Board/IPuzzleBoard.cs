using System.Collections.Generic;

namespace Puzzle.Core
{
    /// <summary>
    /// 보드에서 발생한 상태 변화(매치, 파괴, 이동 등)를 화면(View)에 요청하기 위해 기록하는 데이터 클래스입니다.
    /// </summary>
    public class BoardViewAction
    {
        /// <summary> 해당 연출이 발생한 보드 로직 프레임 (리플레이용) </summary>
        public uint frame;
        /// <summary> 연출이 보여질 순서 번호 (뷰 연출용) </summary>
        public uint orderIndex;
        /// <summary> 연출의 종류 (파괴, 생성, 이동 등) </summary>
        public ViewType type;
        /// <summary> 주가 되는 좌표 (생성 위치, 파괴 위치 등) </summary>
        public GridPos position;
        /// <summary> 이동 시 목적지 좌표 </summary>
        public GridPos targetPosition;
        /// <summary> 생성하거나 파괴한 블럭으로, 파괴 시점의 폭탄 기믹도 포함합니다. </summary>
        public Block blockData;
        /// <summary>피벗이 원래 매칭 범위에 추가한 칸의 파괴 여부입니다.</summary>
        public bool IsPivotNeighbor => type == ViewType.Destroy && blockData != null && blockData.IsPivotTarget;
    }

    /// <summary>한 번의 매칭에서 직접 파괴한 블럭과 그 기믹이 연쇄 파괴한 블럭을 같은 연출 순서로 묶습니다.</summary>
    internal sealed class MatchDestructionGroup
    {
        /// <summary>현재 매칭을 처리하는 동안 보드가 파괴 액션에 부여하는 순서입니다.</summary>
        private uint? _activeOrder;

        /// <summary><paramref name="type"/>의 연출에 <paramref name="customOrder"/> 또는 매칭 순서를 적용하며, 독립 액션은 <paramref name="nextOrder"/>에서 발급합니다.</summary>
        public uint ResolveOrder(ViewType type, uint? customOrder, ref uint nextOrder)
        {
            if (customOrder.HasValue)
            {
                return customOrder.Value;
            }
            if (type == ViewType.Destroy && _activeOrder.HasValue)
            {
                return _activeOrder.Value;
            }
            return nextOrder++;
        }

        /// <summary><paramref name="board"/>의 <paramref name="targets"/>를 차례로 파괴하며 연쇄 기믹까지 <paramref name="order"/>를 공유합니다.</summary>
        public void DestroyMatches(IPuzzleBoard board, IReadOnlyList<GridPos> targets, uint order)
        {
            uint? previousOrder = _activeOrder;
            _activeOrder = order;
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    GridPos pos = targets[i];
                    PuzzleCell cell = board.GetCell(pos);
                    if (cell == null || cell.Block == null)
                    {
                        continue;
                    }

                    Block destroyed = cell.Block;
                    destroyed.SetState(BlockState.Matched);
                    board.Objective.OnBlockDestroyed(destroyed.GetBlockId());
                    cell.Block = null;
                    board.AddView(new BoardViewAction { type = ViewType.Destroy, blockData = destroyed, position = pos });
                    destroyed.FireDestroyed(board, pos);
                }
            }
            finally
            {
                _activeOrder = previousOrder;
            }
        }
    }

    /// <summary>
    /// 퍼즐 보드의 핵심 로직을 정의하는 인터페이스입니다.
    /// 게임 타입(매치3, 육각형, 링크 등)에 따라 다르게 구현될 수 있습니다.
    /// </summary>
    public interface IPuzzleBoard
    {
        /// <summary> 보드의 현재 논리적 상태 </summary>
        BoardState State { get; }

        /// <summary> 게임 내 공용 난수 생성기 </summary>
        PuzzleRandom Random { get; }

        /// <summary> 스테이지 목표 관리자 </summary>
        ObjectiveManager Objective { get; }

        /// <summary> 좌표별 셀 데이터를 저장하는 딕셔너리 </summary>
        Dictionary<GridPos, PuzzleCell> Cells { get; }

        /// <summary> 보드의 가로 너비 </summary>
        int Width { get; }

        /// <summary> 보드의 세로 높이 </summary>
        int Height { get; }

        /// <summary> 게임 사양서를 바탕으로 보드를 초기화합니다. </summary>
        /// <param name="spec">보드 초기화에 사용할 게임 사양</param>
        void Initialize(GameSpec spec);

        /// <summary> 외부에서 입력 좌표를 전달받아 처리 대기열에 추가합니다. </summary>
        /// <param name="input">입력한 격자 좌표</param>
        void Input(GridPos input);

        /// <summary>종료를 대기열에 등록합니다. <paramref name="canceled"/>이면 매칭을 확정하지 않고 선택을 해제합니다.</summary>
        bool InputEnd(bool canceled = false);

        /// <summary> 고정 프레임 간격으로 논리 프레임 및 결정론적 로직을 업데이트합니다. </summary>
        void FixedUpdate();

        /// <summary> 보드의 로직 처리를 일시 정지하거나 재개합니다. </summary>
        /// <param name="pause">일시정지 요청 여부</param>
        void Pause(bool pause);

        /// <summary><paramref name="view"/>에 현재 프레임과 <paramref name="customOrder"/>를 부여합니다. 순서가 생략된 파괴는 진행 중인 매칭의 순서를 사용합니다.</summary>
        public void AddView(BoardViewAction view, uint? customOrder = null);

        /// <summary> 발생한 View 액션 리스트를 가져오고 보드 내역을 비웁니다. (소비형) </summary>
        List<BoardViewAction> FetchActions();

        /// <summary> 지금까지 기록된 유저의 모든 조작 내역을 반환합니다. (리플레이용) </summary>
        List<InputRecord> GetRecordedInputs();

        /// <summary> 지금까지 기록된 유저의 모든 입력 종료 내역을 반환합니다. (리플레이용) </summary>
        List<InputEndRecord> GetRecordedInputEnds();

        /// <summary> 지정된 좌표에 해당하는 셀 객체를 가져옵니다. </summary>
        /// <param name="pos">대상 격자 좌표</param>
        PuzzleCell GetCell(GridPos pos);
    }
}
