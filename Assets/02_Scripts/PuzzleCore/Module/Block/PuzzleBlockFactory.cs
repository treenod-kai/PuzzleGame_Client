using System;
using System.Collections.Generic;

namespace Puzzle.Core
{
    /// <summary>
    /// BlockData로부터 블럭을 생성하고, gimmickIds에 명시된 기믹을 부착하는 팩토리 클래스입니다.
    /// inputType(조작 방법)과 gimmickIds(행동)는 서로 독립적인 데이터입니다.
    /// JSON에는 가독성을 위해 기믹을 문자열로 두고, 내부에서는 GimmickType enum으로 변환해 사용합니다.
    /// 보드 생성 시 인스턴스화되어 캐싱되어 사용됩니다.
    /// </summary>
    public class PuzzleBlockFactory
    {
        /// <summary> 기믹 타입으로부터 기믹 인스턴스를 생성하는 팩토리 </summary>
        private readonly GimmickFactory _gimmickFactory = new GimmickFactory();
        private readonly List<BlockData> _battleBlocks;

        /// <summary>모든 블록 정의를 그대로 사용할 수 있는 팩토리를 만듭니다.</summary>
        public PuzzleBlockFactory()
        {
        }

        /// <summary><paramref name="spec"/>의 대전 규칙에 따라 특수 블록 대신 사용할 일반 블록을 원래 순서로 보관합니다.</summary>
        internal PuzzleBlockFactory(GameSpec spec)
        {
            if (spec.mode != GameMode.Battle || spec.UseLegacyBattleBlocks)
            {
                return;
            }
            _battleBlocks = spec.HasSpecialBlocks ? spec.blocks.FindAll(block => block != null && !block.IsSpecial) : spec.blocks;
            if (_battleBlocks == null || _battleBlocks.Count == 0)
            {
                throw new ArgumentException("대전에 사용할 일반 블록이 없습니다.", nameof(spec));
            }
        }

        /// <summary>현재 모드에서 <paramref name="data"/>로 블록을 생성할 수 있는지 확인합니다.</summary>
        internal bool CanCreate(BlockData data) => data != null && (_battleBlocks == null || !data.IsSpecial);

        /// <summary><paramref name="source"/>를 <paramref name="spec"/>에 맞는 보드 셀로 복사하고,
        /// 대전의 고정 특수 블록은 <paramref name="random"/>으로 고른 일반 블록으로 교체합니다.</summary>
        internal PuzzleCell CreateCell(CellData source, GameSpec spec, PuzzleRandom random)
        {
            PuzzleCell cell = new PuzzleCell(new GridPos(source.x, source.y)) { CellType = (CellType)source.cell_type };
            if (cell.CellType == CellType.Generator && source.generator_block_ids != null && source.generator_block_ids.Count > 0)
            {
                CopyGeneratorBlocks(source.generator_block_ids, cell.generatorBlockIds, spec);
            }
            if (!string.IsNullOrEmpty(source.block_id))
            {
                BlockData data = spec.GetBlock(source.block_id);
                if (data != null && !CanCreate(data))
                {
                    data = _battleBlocks[random.Next(0, _battleBlocks.Count)];
                }
                cell.Block = Create(data);
            }
            return cell;
        }

        /// <summary><paramref name="source"/>의 생성 순서와 가중치를 <paramref name="target"/>에 복사하며,
        /// 대전에서는 <paramref name="spec"/>에서 허용된 블록만 남깁니다.</summary>
        private void CopyGeneratorBlocks(List<string> source, List<string> target, GameSpec spec)
        {
            if (_battleBlocks == null || ReferenceEquals(_battleBlocks, spec.blocks))
            {
                target.AddRange(source);
                return;
            }
            foreach (string id in source)
            {
                if (!string.IsNullOrEmpty(id) && CanCreate(spec.GetBlock(id)))
                {
                    target.Add(id);
                }
            }
            if (target.Count == 0)
            {
                // 특수 블록 전용 생성기도 대전에서 해당 열을 계속 채울 수 있게 합니다.
                foreach (BlockData block in _battleBlocks)
                {
                    target.Add(block.blockId);
                }
            }
        }

        /// <summary>
        /// 데이터로부터 블럭을 생성하고, gimmickIds(문자열)를 GimmickType으로 변환해 기믹을 부착합니다.
        /// </summary>
        /// <param name="data">블럭 설정 데이터</param>
        /// <returns>생성된 블럭 객체</returns>
        public Block Create(BlockData data)
        {
            if (!CanCreate(data))
            {
                return null;
            }

            // 모든 블럭은 단일 클래스(Block)로 생성한다.
            Block block = new Block(data);

            // 데이터에 명시된 기믹 문자열 id들을 enum으로 변환해 부착한다.
            if (data.gimmickIds != null)
            {
                for (int i = 0; i < data.gimmickIds.Count; i++)
                {
                    // 데이터는 문자열(가독용), 내부 처리는 enum(int)으로 변환
                    if (Enum.TryParse(data.gimmickIds[i], true, out GimmickType type))
                    {
                        IGimmick gimmick = _gimmickFactory.Create(type);
                        if (gimmick != null)
                        {
                            block.AddGimmick(gimmick);
                        }
                    }
                }
            }

            return block;
        }
    }
}
