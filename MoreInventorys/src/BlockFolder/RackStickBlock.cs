using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace MoreInventorys.src.BlockFolder
{
    internal class RackStickBlock : Block
    {
        WorldInteraction[]? interactions;
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);

            PlacedPriorityInteract = true;

            interactions = ObjectCacheUtil.GetOrCreate(api, "rackStickInteractions", () =>
            {
                List<ItemStack> containerStacklist = new List<ItemStack>();

                foreach (var kvp in ModConfigFile.Current.ModedStorageContainersCode)
                {
                    if (kvp.Key != "mibasketclosed") continue;
                    var block = api.World.GetBlock(new AssetLocation("moreinventorys:" + kvp.Key + "-east"));
                    if (block != null)
                    {
                        containerStacklist.Add(new ItemStack(block));
                    }
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                            ActionLangCode = "Поставить в стеллаж", // Текст напрямую
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = containerStacklist.ToArray(),
                            ShouldApply = (wi, bs, es) =>
                            {
                                var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BERackStick;
                                if (be == null) return false;
                                return be.Inventory[bs.SelectionBoxIndex].Empty;
                            }}
                    };
            });

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine("Слотов для контейнеров: 4");
            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("moreinventorys:block-rackstick-desc"));
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var worldInteractions = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            var resp = world.Claims.TestAccess(forPlayer, selection.Position, EnumBlockAccessFlags.Use);
            if (resp == EnumWorldAccessResponse.Granted && interactions != null)
            {
                return worldInteractions?.Concat(interactions).ToArray() ?? interactions;
            }

            return worldInteractions ?? new WorldInteraction[0];
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
            }, 1); 
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
