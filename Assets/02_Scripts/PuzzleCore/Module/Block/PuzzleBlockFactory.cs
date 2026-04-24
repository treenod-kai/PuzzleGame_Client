using System;

namespace Puzzle.Core
{
    /// <summary>
    /// BlockData의 설정에 따라 적절한 PuzzleBlock의 파생 객체를 생성하는 팩토리 클래스입니다.
    /// 보드 생성 시 인스턴스화되어 캐싱되어 사용됩니다.
    /// </summary>
    public class PuzzleBlockFactory
    {
        /// <summary>
        /// 데이터를 분석하여 터치 가능한지, 스왑 가능한지에 따라 알맞은 블럭 객체를 생성합니다.
        /// </summary>
        /// <param name="data">블럭 설정 데이터</param>
        /// <returns>생성된 구체적인 블럭 객체</returns>
        public BaseBlock Create(BlockData data)
        {
            if (data == null)
            {
                return null;
            }

            bool isTouchable = data.inputType.HasFlag(InputType.Touch);
            bool isSwappable = data.inputType.HasFlag(InputType.Swap);

            if (isTouchable && isSwappable)
            {
                return new BombBlock(data);
            }
            else if (isSwappable)
            {
                return new NormalBlock(data);
            }
            
            return new NormalBlock(data);
        }
    }
}
