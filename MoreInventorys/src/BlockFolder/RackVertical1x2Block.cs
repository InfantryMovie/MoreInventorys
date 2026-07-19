using Microsoft.VisualBasic;
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
    internal class RackVertical1x2Block : Block
    {
        WorldInteraction[]? interactions;
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);

            PlacedPriorityInteract = true;

            interactions = ObjectCacheUtil.GetOrCreate(api, "rackvertical1x2Interactions", () =>
            {
                List<ItemStack> containerStacklist = new List<ItemStack>();

                foreach (var code in ModConfigFile.Current.VanilaStorageContainersCode)
                {
                    var block = api.World.GetBlock(new AssetLocation("game:" + code + "-east"));
                    if (block != null && !block.Code.ToString().Contains("trunk"))
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
                           ActionLangCode = Lang.Get("moreinventorys:block-rack-action-place"),
                            MouseButton = EnumMouseButton.Right,
                            Itemstacks = containerStacklist.ToArray(),
                            ShouldApply = (wi, bs, es) =>
                            {
                                var be = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BERackVertical1x2;
                                if (be == null) return false;
                                return be.Inventory[bs.SelectionBoxIndex].Empty;
                            }}
                    };
            });

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get("moreinventorys:block-rackvertical-desc-storage"));
            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("moreinventorys:block-rackvertical-desc"));
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
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BERackVertical1x2 be)
            {
                return be.OnBlockInteract(byPlayer, blockSel);
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            BlockPos upperblock1 = blockSel.Position.UpCopy();


            if (world.BlockAccessor.GetBlockId(upperblock1) != 0) return false;


            bool ret = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (!ret) return false;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is BERackVertical1x2 rackBe)
            {
                rackBe.DummyPositions.Clear();
                rackBe.DummyPositions.Add(upperblock1);
            }

            SetDummyBlock(world, upperblock1, blockSel.Position);

            return ret;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {

            var be = world.BlockAccessor.GetBlockEntity(pos) as BERackVertical1x2;
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
