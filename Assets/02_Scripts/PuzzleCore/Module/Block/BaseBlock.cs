namespace Puzzle.Core
{
    #region Interfaces
    /// <summary> 
    /// 터치(클릭) 조작을 받을 수 있는 블럭 능력을 정의합니다. 
    /// </summary>
    public interface ITouchableBlock
    {
        /// <summary> 블럭이 터치되었을 때 실행될 로직 </summary>
        void OnTouched(IPuzzleBoard board, GridPos myPos);
    }

    /// <summary> 
    /// 스왑(드래그 등) 조작을 받을 수 있는 블럭 능력을 정의합니다. 
    /// </summary>
    public interface ISwappableBlock
    {
        /// <summary> 블럭이 다른 블럭과 교체되었을 때 실행될 로직 </summary>
        bool OnSwapped(IPuzzleBoard board, GridPos myPos, GridPos targetPos);
    }

    /// <summary> 
    /// 선 긋기(Link) 조작을 받을 수 있는 블럭 능력을 정의합니다. 
    /// </summary>
    public interface ILinkableBlock
    {
        /// <summary> 이전 블럭과 현재 블럭이 연결 가능한지 확인 </summary>
        bool CanLink(IPuzzleBoard board, GridPos myPos, GridPos previousPos);
    }
    #endregion

    /// <summary>
    /// 셀 위에 놓여 유저가 조작하거나 매치되는 퍼즐 조각의 기본 추상 클래스입니다.
    /// </summary>
    public abstract class BaseBlock
    {
        /// <summary> 블럭의 속성 정보를 담고 있는 데이터 객체 </summary>
        protected BlockData _blockData;

        /// <summary> 블럭의 현재 논리적 상태 </summary>
        public BlockState State { get; protected set; } = BlockState.Idle;

        /// <summary>
        /// 블럭의 상태를 변경합니다.
        /// </summary>
        /// <param name="newState">새로운 상태</param>
        public virtual void SetState(BlockState newState)
        {
            State = newState;
        }

        /// <summary>
        /// 블럭의 고유 아이디를 반환합니다.
        /// </summary>
        /// <returns>블럭 아이디</returns>
        public string GetBlockId()
        {
            return _blockData?.blockId;
        }

        /// <summary>
        /// 지정된 데이터를 사용하여 새로운 블럭 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="data">블럭 설정 데이터</param>
        public BaseBlock(BlockData data)
        {
            _blockData = data;
        }

        /// <summary>
        /// 매 프레임마다 블럭의 상태를 업데이트합니다.
        /// </summary>
        /// <param name="board">현재 보드 객체</param>
        /// <param name="myPos">블럭의 현재 위치</param>
        internal virtual void Update(IPuzzleBoard board, GridPos myPos)
        {
        }
    }
}
