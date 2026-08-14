using MoreInventorys.src.GuiFolder;
using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace MoreInventorys.src.BlockEntityFolder
{
    public class BEClothingRack : BlockEntityDisplay
    {
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "clothingrackInventory";
        public override string AttributeTransformCode => "onClothingrackTransform";
        Block block;
        static int slotCount = 6;
        public override int DisplayedItems => 6;

        public BEClothingRack()
        {
            inv = new InventoryGeneric(slotCount, "clothingrack-0", null);
        }

        public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Empty)
            {
                if (TryTake(byPlayer, blockSel))
                {
                    return true;
                }
                return false;
            }

            if (!IsValidWClothing(slot)) return false;

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            if (TryPut(slot, blockSel))
            {
                MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);
                MarkDirty();
                return true;
            }
            return false;
        }

        public bool IsValidWClothing(ItemSlot slot)
        {
            if (slot.Itemstack.Item == null) return false;
            bool isValidTag = false;
            var code = slot.Itemstack.Item.Code.Path;
            if (code.StartsWith("clothes") || code.StartsWith("armor-body")) isValidTag = true;
            if ((code.Contains("shoulder") || code.Contains("upperbody") || code.StartsWith("armor-body")) && isValidTag)
            {
                return true;
            }


            return false;
        }

        string GetValueBeforeDash(string input)
        {
            int indexOfDash = input.IndexOf('-');

            if (indexOfDash >= 0)
            {
                return input.Substring(0, indexOfDash);
            }

            return input;
        }

        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            if (inv[blockSel.SelectionBoxIndex].Empty)
            {
                int num = slot.TryPutInto(Api.World, inv[blockSel.SelectionBoxIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }
            return false;
        }

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!inv[blockSel.SelectionBoxIndex].Empty)
            {
                ItemStack stack = inv[blockSel.SelectionBoxIndex].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    MoreInventorysMod.PlaySoundBlockAt(Api, stack, byPlayer);
                }
                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();
                return true;
            }
            return false;
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];
            int orientationRotate = 0;

            if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
            if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
            if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;

            for (int index = 0; index < slotCount; index++)
            {
                var clothing = inv[index];
                var code = clothing.Itemstack?.Item?.Code?.Path;
                float scalx = 0.3f;
                float scaly = 1f;
                float scalz = 1f;

                if (string.IsNullOrEmpty(code))
                {
                    tfMatrices[index] = new Matrixf()
                        .Scale(0.1f, 0.1f, 0.1f)
                        .Values;
                    continue;
                }

                float x = index * 0.125f + 0.52f;
                float z = 0.401f;
                float y = 0.19f;

                if(code != null)
                {
                    if(code.Contains("shoulder-survivor")|| code.Contains("shoulder-miner") || code.Contains("shoulder-malefactor-cloak") ||
                        code.Contains("shoulder-marketeer"))
                    {
                        y -= 0.33f;
                    }
                }


                tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalx, scaly, scalz)
                       .Values;
            }

            return tfMatrices;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RedrawAfterReceivingTreeAttributes(worldForResolving);
            DummyPositions = new List<BlockPos>();
            int count = tree.GetInt("dummyCount");
            for (int i = 0; i < count; i++)
            {
                DummyPositions.Add(new BlockPos(tree.GetInt("dx" + i), tree.GetInt("dy" + i), tree.GetInt("dz" + i)));
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("dummyCount", DummyPositions.Count);
            for (int i = 0; i < DummyPositions.Count; i++)
            {
                tree.SetInt("dx" + i, DummyPositions[i].X);
                tree.SetInt("dy" + i, DummyPositions[i].Y);
                tree.SetInt("dz" + i, DummyPositions[i].Z);
            }
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            sb.AppendLine();
            for (int i = 0; i < inv.Count; ++i)
            {
                if (!inv[i].Empty)
                {
                    ItemStack stack = inv[i].Itemstack;
                    sb.AppendLine(stack.GetName());
                }
            }
        }
    }
}