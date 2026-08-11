using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockFolder
{
    public class ClothingRackBlock : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);
            // Todo: Add interaction help

        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get("moreinventorys:block-clothingrack-desc-storage"));
            dsc.AppendLine(Lang.Get("moreinventorys:block-clothingrack-desc"));


        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            BEClothingRack be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEClothingRack;

            if (be != null) return be.OnInteract(byPlayer, blockSel);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        private void SetDummyBlock(IWorldAccessor world, BlockPos dummyPos, BlockPos mainPos)
        {
            Block dummyBlock = world.GetBlock(new AssetLocation("moreinventorys:dummydrawer"));
            world.BlockAccessor.SetBlock(dummyBlock.BlockId, dummyPos);

            world.RegisterCallback((dt) =>
            {
                var dummyBe = world.BlockAccessor.GetBlockEntity(dummyPos) as BlockEntityDummyDrawer;
                if (dummyBe != null)
                {
                    dummyBe.MainBlockPos = mainPos;
                    dummyBe.MarkDirty(true);
                }
            }, 1);
        }



        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {

            var be = world.BlockAccessor.GetBlockEntity(pos) as BEClothingRack;
            if (be != null)
            {
                foreach (var dummy in be.DummyPositions)
                {
                    world.BlockAccessor.SetBlock(0, dummy);
                }
            }

            base.OnBlockRemoved(world, pos);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos upperblock1 = blockSel.Position.UpCopy();


            if (world.BlockAccessor.GetBlockId(upperblock1) != 0) return false;


            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (!ret) return false;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is BEClothingRack largeBe)
            {
                largeBe.DummyPositions.Clear();
                largeBe.DummyPositions.Add(upperblock1);
            }

            SetDummyBlock(world, upperblock1, blockSel.Position);

            return ret;
        }


    }

}
