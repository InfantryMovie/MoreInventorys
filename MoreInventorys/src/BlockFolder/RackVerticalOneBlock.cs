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
    internal class RackVerticalOneBlock : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);
            // Todo: Add interaction help

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERackVerticalOne be)
            {
                return be.OnBlockInteract(byPlayer);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos upperPart1 = blockSel.Position.UpCopy();
            BlockPos upperPart2 = blockSel.Position.UpCopy(2);
            if (world.BlockAccessor.GetBlockId(upperPart1) != 0 || world.BlockAccessor.GetBlockId(upperPart2) != 0) return false;

            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            Block upperBlock = world.GetBlock(new AssetLocation("moreinventorys:dummy-up"));
            Block upperBlock2 = world.GetBlock(new AssetLocation("moreinventorys:dummy-up"));
            world.BlockAccessor.SetBlock(upperBlock.BlockId, upperPart1);
            world.BlockAccessor.SetBlock(upperBlock2.BlockId, upperPart2);

            return ret;
        }
        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            world.BlockAccessor.SetBlock(0, pos.UpCopy());
            world.BlockAccessor.SetBlock(0, pos.UpCopy(2)); 
            base.OnBlockRemoved(world, pos);
        }
    }
}
