using System;

namespace Puzzle.Core
{
    /// <summary>
    /// 터치하면 즉시 폭발하거나 스왑 시 연쇄 효과를 내는 폭탄 블럭입니다.
    /// </summary>
    public class BombBlock : BaseBlock, ITouchableBlock, ISwappableBlock
    {
        public BombBlock(BlockData data) : base(data)
        {
        }

        public void OnTouched(IPuzzleBoard board, GridPos myPos)
        {
            var cell = board.GetCell(myPos);
            if (cell != null)
            {
                cell.Block = null;
                board.AddView(new BoardViewAction { type = ViewType.Destroy, frame = 0, position = myPos });
            }
        }

        public bool OnSwapped(IPuzzleBoard board, GridPos myPos, GridPos targetPos)
        {
            var targetCell = board.GetCell(targetPos);
            if (targetCell != null)
            {
                targetCell.Block = null;
                board.AddView(new BoardViewAction { type = ViewType.Destroy, frame = 0, position = targetPos });
            }
            return true;
        }

        internal override void Update(IPuzzleBoard board, GridPos myPos)
        {
            base.Update(board, myPos);
        }
    }
}
