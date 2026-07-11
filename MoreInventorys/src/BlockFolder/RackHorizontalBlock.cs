using Microsoft.VisualBasic;
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
        WorldInteraction[]? interactions;
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);

            PlacedPriorityInteract = true;

            interactions = ObjectCacheUtil.GetOrCreate(api, "rackhorizontalInteractions", () =>
            {
                List<ItemStack> containerStacklist = new List<ItemStack>();

                foreach (var code in ModConfigFile.Current.VanilaStorageContainersCode)
                {
                    var block = api.World.GetBlock(new AssetLocation("game:" + code + "-east"));
                    if (block != null)
                    {
                        var stack = new ItemStack(block);
                        string type = block.Attributes?["defaultType"]?.AsString();
                        if (!string.IsNullOrEmpty(type))
                        {
                            stack.Attributes.SetString("type", type);
                        }
                        containerStacklist.Add(stack);
                    }
                }

                foreach (var kvp in ModConfigFile.Current.ModedStorageContainersCode)
                {
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
                                var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BERackHorizontal;
                                if (be == null) return false;
                                return be.Inventory[bs.SelectionBoxIndex].Empty;
                            }}
                    };
                });

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("moreinventorys:block-rackhorizontal-desc"));
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
