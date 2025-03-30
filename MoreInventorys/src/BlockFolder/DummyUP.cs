using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockFolder
{
    class DummyUP : Block
    {
        /* добавить в Класс блока в который будем ставить дамми блоки:
         
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            //проверка есть ли сверху предмет, перед  тем как поставить туда дамми блок
            //-----------------------
            BlockPos upperPart = blockSel.Position.UpCopy();
            if (world.BlockAccessor.GetBlockId(upperPart) != 0) return false;
            //-----------------------

            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            Block upperBlock = world.GetBlock(new AssetLocation("moreinventorys:dummy-up"));
            world.BlockAccessor.SetBlock(upperBlock.BlockId, upperPart);

            return ret;
        }
        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            world.BlockAccessor.SetBlock(0, pos.UpCopy());
            base.OnBlockRemoved(world, pos);
        }*/

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
            Block biq = blockAccessor.GetBlock(pos.DownCopy());
            if (biq.Id == 0) return SelectionBoxes;
            Cuboidf[] cuboidfs = biq.GetSelectionBoxes(blockAccessor, pos.DownCopy());
            Cuboidf[] cuboidret = new Cuboidf[cuboidfs.Length];
            for (int i = 0; i < cuboidfs.Length; i++)
            {
                cuboidret[i] = cuboidfs[i].OffsetCopy(0, -1, 0);
            }
            return cuboidret;
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block biq = blockAccessor.GetBlock(pos.DownCopy());
            if (biq.Id == 0) return CollisionBoxes;
            Cuboidf[] cuboidfs = biq.GetCollisionBoxes(blockAccessor, pos.DownCopy());
            Cuboidf[] cuboidret = new Cuboidf[cuboidfs.Length];
            for (int i = 0; i < cuboidfs.Length; i++)
            {
                cuboidret[i] = cuboidfs[i].OffsetCopy(0, -1, 0);
            }
            return cuboidret;
        }
    }
}
