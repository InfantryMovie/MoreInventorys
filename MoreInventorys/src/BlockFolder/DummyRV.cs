using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockFolder
{
    public class DummyRV : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
             BlockSelection downSelection = new BlockSelection
             {
                 Position = blockSel.Position.DownCopy()
             };
             return world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).OnBlockInteractStart(world, byPlayer, downSelection);

        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
             world.BlockAccessor.BreakBlock(pos.DownCopy(), byPlayer);
             world.BlockAccessor.SetBlock(0, pos);
        }



        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            return world.BlockAccessor.GetBlock(pos.DownCopy()).GetPlacedBlockName(world, pos.DownCopy());
           
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            return world.BlockAccessor.GetBlock(pos.DownCopy()).GetPlacedBlockInfo(world, pos.DownCopy(), forPlayer);
            
        }


        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityDummy;
            if (be == null) return base.GetSelectionBoxes(blockAccessor, pos);

            Block mainBlock = blockAccessor.GetBlock(be.MainBlockPos);
            if (mainBlock == null) return base.GetSelectionBoxes(blockAccessor, pos);

            var boxes = mainBlock.GetSelectionBoxes(blockAccessor, be.MainBlockPos);
            if (boxes == null)
            {
                return base.GetSelectionBoxes(blockAccessor, pos);
            }
            Cuboidf[] offsetBoxes = new Cuboidf[boxes.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                offsetBoxes[i] = boxes[i].OffsetCopy(
                    be.MainBlockPos.X - pos.X,
                    be.MainBlockPos.Y - pos.Y,
                    be.MainBlockPos.Z - pos.Z
                );
            }

            return offsetBoxes;
        }
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityDummy;
            if (be == null) return base.GetSelectionBoxes(blockAccessor, pos);

            Block mainBlock = blockAccessor.GetBlock(be.MainBlockPos);
            if (mainBlock == null) return base.GetSelectionBoxes(blockAccessor, pos);

            var boxes = mainBlock.GetSelectionBoxes(blockAccessor, be.MainBlockPos);
            if (boxes == null)
            {
                return base.GetSelectionBoxes(blockAccessor, pos);
            }

            Cuboidf[] offsetBoxes = new Cuboidf[boxes.Length];
            for (int i = 0; i < boxes.Length; i++)
            {
                offsetBoxes[i] = boxes[i].OffsetCopy(
                    be.MainBlockPos.X - pos.X,
                    be.MainBlockPos.Y - pos.Y,
                    be.MainBlockPos.Z - pos.Z
                );
            }

            return offsetBoxes;
        }
    }
}
