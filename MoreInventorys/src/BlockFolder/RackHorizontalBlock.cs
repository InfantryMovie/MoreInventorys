using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoreInventorys.src.BlockFolder
{
    internal class RackHorizontalBlock : Block
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


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERackHorizontal be)
            {
                return be.OnBlockInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        BlockPos GetRightBlockPos(BlockSelection blockSel, IPlayer byPlayer)
        {
            BlockPos selPos = blockSel.Position;

            // Получаем угол взгляда в радианах
            float yaw = byPlayer.Entity.Pos.Yaw;

            // Вычисляем направление ВПРАВО (поворот на -90° для системы координат VintageStory)
            float rightYaw = yaw - (float)Math.PI / 2;

            // Получаем компоненты направления
            float dx = (float)Math.Sin(rightYaw);
            float dz = (float)Math.Cos(rightYaw);

            // Округляем до -1, 0 или 1
            int roundedDx = dx > 0.5f ? 1 : (dx < -0.5f ? -1 : 0);
            int roundedDz = dz > 0.5f ? 1 : (dz < -0.5f ? -1 : 0);

            // Если получилось (0,0) из-за округления - берём восток по умолчанию
            if (roundedDx == 0 && roundedDz == 0)
            {
                return selPos.AddCopy(1, 0, 0);
            }

            return selPos.AddCopy(roundedDx, 0, roundedDz);
        }

        private string GetFacing(float yaw)
        {
            // Приводим угол к диапазону 0-360
            float degrees = (yaw * 180 / (float)Math.PI + 360) % 360;

            // Определяем направление
            if (degrees >= 45 && degrees < 135) return "east";
            if (degrees >= 135 && degrees < 225) return "south";
            if (degrees >= 225 && degrees < 315) return "west";
            return "north";
        }
        /*BlockPos GetRightBlockPos(BlockSelection blockSel, IPlayer byPlayer)
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
        }*/
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos upperPart1 = blockSel.Position.UpCopy();
            BlockPos upperPart2 = blockSel.Position.UpCopy(2);
            BlockPos rightBlockpos = GetRightBlockPos(blockSel, byPlayer);
            BlockPos rightUp1 = rightBlockpos.UpCopy();
            BlockPos rightUp2 = rightBlockpos.UpCopy(2);

            if (world.BlockAccessor.GetBlockId(upperPart1) != 0 || world.BlockAccessor.GetBlockId(upperPart2) != 0
                || world.BlockAccessor.GetBlockId(rightBlockpos) != 0 || world.BlockAccessor.GetBlockId(rightUp1) != 0
                || world.BlockAccessor.GetBlockId(rightUp2) != 0) return false;


            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (!ret) return false;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is BERackHorizontal rackBe)
            {
                rackBe.DummyPositions.Clear();
                rackBe.DummyPositions.Add(upperPart1);
                rackBe.DummyPositions.Add(upperPart2);
                rackBe.DummyPositions.Add(rightBlockpos);
                rackBe.DummyPositions.Add(rightUp1);
                rackBe.DummyPositions.Add(rightUp2);
            }

            SetDummyBlock(world, upperPart1, blockSel.Position);
            SetDummyBlock(world, upperPart2, blockSel.Position);
            SetDummyBlock(world, rightBlockpos, blockSel.Position);
            SetDummyBlock(world, rightUp1, blockSel.Position);
            SetDummyBlock(world, rightUp2, blockSel.Position);

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
            }, 1);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {

            var be = world.BlockAccessor.GetBlockEntity(pos) as BERackHorizontal;
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
