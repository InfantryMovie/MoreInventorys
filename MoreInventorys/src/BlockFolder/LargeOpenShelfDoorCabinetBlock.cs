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
    public class LargeOpenShelfDoorCabinetBlock : Block
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
            dsc.AppendLine(Lang.Get("moreinventorys:block-largeopenshelfdoorcabinet-desc-storage"));
            dsc.AppendLine(Lang.Get("moreinventorys:block-largeopenshelfdoorcabinet-desc"));


        }

        private void SetDummyBlock(IWorldAccessor world, BlockPos dummyPos, BlockPos mainPos)
        {
            Block dummyBlock = world.GetBlock(new AssetLocation("moreinventorys:dummydrawer"));
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

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {

            var be = world.BlockAccessor.GetBlockEntity(pos) as BELargeOpenShelfDoorCabinet;
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
            BlockPos rightBlockpos = GetRightBlockPos(blockSel, byPlayer);


            if (world.BlockAccessor.GetBlockId(rightBlockpos) != 0) return false;


            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (!ret) return false;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is BELargeOpenShelfDoorCabinet largeBe)
            {
                largeBe.DummyPositions.Clear();
                largeBe.DummyPositions.Add(rightBlockpos);
            }

            SetDummyBlock(world, rightBlockpos, blockSel.Position);

            return ret;
        }




        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BELargeOpenShelfDoorCabinet be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BELargeOpenShelfDoorCabinet;

            if (be != null)
            {
                // Просто открываем GUI (ящик)
                return be.OnInteract(byPlayer, blockSel);
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

}
