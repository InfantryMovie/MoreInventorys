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
    internal class RackStickBlock : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);
            // Todo: Add interaction help

        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERackStick be)
            {
                return be.OnBlockInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
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
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos upperblock1 = blockSel.Position.UpCopy();
            BlockPos rightBlock1 = GetRightBlockPos(blockSel, byPlayer);
            BlockPos rightBlockUp1 = rightBlock1.UpCopy();

            if (world.BlockAccessor.GetBlockId(upperblock1) != 0 
                || world.BlockAccessor.GetBlockId(rightBlock1) != 0
                || world.BlockAccessor.GetBlockId(rightBlockUp1) != 0) return false;


            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (!ret) return false;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is BERackStick rackBe)
            {
                rackBe.DummyPositions.Clear();
                rackBe.DummyPositions.Add(upperblock1);
                rackBe.DummyPositions.Add(rightBlock1);
                rackBe.DummyPositions.Add(rightBlockUp1);
            }

            SetDummyBlock(world, upperblock1, blockSel.Position);
            SetDummyBlock(world, rightBlock1, blockSel.Position);
            SetDummyBlock(world, rightBlockUp1, blockSel.Position);

            return ret;
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
            }, 1); // 50 мс задержка — можно варьировать
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {

            var be = world.BlockAccessor.GetBlockEntity(pos) as BERackStick;
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
