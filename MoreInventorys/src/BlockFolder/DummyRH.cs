using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockFolder
{
    public class DummyRH : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityDummy be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityDummy;
            if (be?.MainBlockPos == null) return false;

            BlockSelection mainSel = new BlockSelection
            {
                Position = be.MainBlockPos,
                SelectionBoxIndex = blockSel.SelectionBoxIndex,
                HitPosition = blockSel.HitPosition,
                Face = blockSel.Face
            };

            return world.BlockAccessor.GetBlock(be.MainBlockPos).OnBlockInteractStart(world, byPlayer, mainSel);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BlockEntityDummy be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityDummy;
            if (be?.MainBlockPos != null)
            {
                world.BlockAccessor.BreakBlock(be.MainBlockPos, byPlayer);
            }
            world.BlockAccessor.SetBlock(0, pos);
        }

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            BlockEntityDummy be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityDummy;
            return be?.MainBlockPos != null
                ? world.BlockAccessor.GetBlock(be.MainBlockPos).GetPlacedBlockName(world, be.MainBlockPos)
                : base.GetPlacedBlockName(world, pos);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            BlockEntityDummy be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityDummy;
            return be?.MainBlockPos != null
                ? world.BlockAccessor.GetBlock(be.MainBlockPos).GetPlacedBlockInfo(world, be.MainBlockPos, forPlayer)
                : base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity(pos) as BlockEntityDummy;
            if (be == null) return base.GetSelectionBoxes(blockAccessor, pos);

            Block mainBlock = blockAccessor.GetBlock(be.MainBlockPos);
            if (mainBlock == null) return base.GetSelectionBoxes(blockAccessor, pos);

            var boxes = mainBlock.GetSelectionBoxes(blockAccessor, be.MainBlockPos);
            if (boxes == null) return base.GetSelectionBoxes(blockAccessor, pos);

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
            if (be == null) return base.GetCollisionBoxes(blockAccessor, pos);

            Block mainBlock = blockAccessor.GetBlock(be.MainBlockPos);
            if (mainBlock == null) return base.GetCollisionBoxes(blockAccessor, pos);

            var boxes = mainBlock.GetCollisionBoxes(blockAccessor, be.MainBlockPos);
            if (boxes == null) return base.GetCollisionBoxes(blockAccessor, pos);

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

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityDummy;
            if (be?.MainBlockPos == null) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            BlockSelection mainSel = new BlockSelection
            {
                Position = be.MainBlockPos,
                SelectionBoxIndex = selection.SelectionBoxIndex,
                HitPosition = selection.HitPosition,
                Face = selection.Face
            };

            Block mainBlock = world.BlockAccessor.GetBlock(be.MainBlockPos);
            if (mainBlock == null) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            return mainBlock.GetPlacedBlockInteractionHelp(world, mainSel, forPlayer);
        }
    }
}