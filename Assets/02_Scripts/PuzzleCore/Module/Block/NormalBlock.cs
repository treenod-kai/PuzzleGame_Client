using System;

namespace Puzzle.Core
{
    /// <summary>
    /// 3매치 등에서 스왑만 가능한 가장 기본적인 일반 블럭입니다.
    /// </summary>
    public class NormalBlock : BaseBlock, ISwappableBlock, ILinkableBlock
    {
        public NormalBlock(BlockData data) : base(data)
        {
        }

        public bool OnSwapped(IPuzzleBoard board, GridPos myPos, GridPos targetPos)
        {
            // 기본적인 스왑 허용
            return true;
        }

        public bool CanLink(IPuzzleBoard board, GridPos myPos, GridPos previousPos)
        {
            // 기본적인 링크 허용 (구체적인 조건은 보드 로직에서 처리됨)
            return true;
        }

        internal override void Update(IPuzzleBoard board, GridPos myPos)
        {
            base.Update(board, myPos);
        }
    }
}
