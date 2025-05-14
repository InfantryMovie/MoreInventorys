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
    internal class RackVerticalBlock : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);

        }
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        BlockPos GetRightBlockPos(BlockSelection blockSel, IPlayer byPlayer)
        {
            BlockPos selPos = blockSel.Position;
            // Получаем угол вращения игрока
            float rotationYaw = byPlayer.Entity.BodyYaw;

            // Коэффициенты смещения вправо и вперёд
            float deltaX = (float)Math.Cos(rotationYaw);
            float deltaZ = (float)Math.Sin(rotationYaw);

            float dx = deltaZ;
            float dz = +deltaX;

            // Формируем позицию правого блока
            BlockPos rightBlockpos = new BlockPos(selPos.X + (int)Math.Round(dx), selPos.Y, selPos.Z + (int)Math.Round(dz));
            return rightBlockpos;
        }

        private void SetDummyBlock(IWorldAccessor world, BlockPos dummyPos, BlockPos mainPos)
        {
            Block dummyBlock = world.GetBlock(new AssetLocation("moreinventorys:dummyrh"));
            world.BlockAccessor.SetBlock(dummyBlock.BlockId, dummyPos);

            world.RegisterCallback((dt) =>
            {
                var dummyBe = world.BlockAccessor.GetBlockEntity(dummyPos) as BlockEntityDummy;
                if (dummyBe != null)
                {
                    dummyBe.MainBlockPos = mainPos;
                    dummyBe.MarkDirty(true);
                }
            }, 1); 
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERackVertical be)
            {
                return be.OnBlockInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos upperblock1 = blockSel.Position.UpCopy();
            BlockPos upperblock2 = blockSel.Position.UpCopy(2);


            if (world.BlockAccessor.GetBlockId(upperblock1) != 0 || world.BlockAccessor.GetBlockId(upperblock2) != 0) return false;


            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (!ret) return false;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is BERackVertical rackBe)
            {
                rackBe.DummyPositions.Clear();
                rackBe.DummyPositions.Add(upperblock1);
                rackBe.DummyPositions.Add(upperblock2);
            }

            SetDummyBlock(world, upperblock1, blockSel.Position);
            SetDummyBlock(world, upperblock2, blockSel.Position);

            return ret;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {

            var be = world.BlockAccessor.GetBlockEntity(pos) as BERackVertical;
            if (be != null)
            {
                foreach (var dummy in be.DummyPositions)
                {
                    world.BlockAccessor.SetBlock(0, dummy);
                }
            }

            base.OnBlockRemoved(world, pos);
        }
       
    }
}
