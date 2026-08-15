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
        static int slotCount = 12;
        public override int DisplayedItems => 12;

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

            if (!IsValidWClothing(slot, blockSel)) return false;

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            if (TryPut(slot, blockSel))
            {
                MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);
                MarkDirty();
                return true;
            }
            return false;
        }

        public bool IsValidWClothing(ItemSlot slot, BlockSelection blockSel)
        {
            if (slot.Itemstack.Item == null) return false;
            bool isValidTag = false;
            var code = slot.Itemstack.Item.Code.Path;
            if(blockSel.SelectionBoxIndex <= 5)
            {
                if (code.StartsWith("clothes") || code.StartsWith("armor-body")) isValidTag = true;
                if ((code.Contains("shoulder") || code.Contains("upperbody") || code.StartsWith("armor-body")) && isValidTag)
                {
                    return true;
                }
            }
            else
            {
                if (code.Contains("foot"))
                {
                    return true;
                }
            }
           


            return false;
        }

        bool TakeBootsSlot(IPlayer byPlayer, int blockSel)
        {
            ItemStack stack = inv[blockSel].TakeOut(1);
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

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!inv[blockSel.SelectionBoxIndex].Empty && blockSel.SelectionBoxIndex < 6)
            {
                return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
            }
            else if (blockSel.SelectionBoxIndex >= 6)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 6:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex+3].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex+3);
                        }
                        break;
                    case 7:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 3].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 3);
                        }
                        break;
                    case 8:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 3].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 3);
                        }
                        break;

                    default:
                        break;
                }
            }
            return false;
        }
        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            if (inv[blockSel.SelectionBoxIndex].Empty && blockSel.SelectionBoxIndex < 6)
            {
                int num = slot.TryPutInto(Api.World, inv[blockSel.SelectionBoxIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }
            else 
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 6:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutBootsSlot(slot, blockSel);
                        }
                        else if (inv[blockSel.SelectionBoxIndex+3].Empty)
                        {
                            return PutBootsSlot(slot, blockSel);
                        }
                        break;
                    case 7:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutBootsSlot(slot, blockSel);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 3].Empty)
                        {
                            return PutBootsSlot(slot, blockSel);
                        }
                        break;
                    case 8:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutBootsSlot(slot, blockSel);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 3].Empty)
                        {
                            return PutBootsSlot(slot, blockSel);
                        }
                        break;

                    default:
                        break;
                }
            }
            return false;
        }

        bool PutBootsSlot(ItemSlot slot, BlockSelection blockSel)
        {
            if (inv[blockSel.SelectionBoxIndex].Empty)
            {
                int num = slot.TryPutInto(Api.World, inv[blockSel.SelectionBoxIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }
            else if (inv[blockSel.SelectionBoxIndex + 3].Empty)
            {
                int num = slot.TryPutInto(Api.World, inv[blockSel.SelectionBoxIndex + 3]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
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

                float scalxBoots = 0.56f;
                float scalyBoots = 0.5f;
                float scalzBoots = 0.5f;

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

                float yboots = 0.29f;
                float zboots = 0f;
                float xboots = 0f;
                if (code != null)
                {
                    if(code.Contains("shoulder-survivor")|| code.Contains("shoulder-miner") || code.Contains("shoulder-malefactor-cloak") ||
                        code.Contains("shoulder-marketeer"))
                    {
                        y -= 0.33f;
                    }
                }

                //-----------ботинки-----------//
                if (index >= 6 && index < 9)
                {
                    zboots = 1.3f;
                    xboots = 0.445f + (index - 6) * 0.27f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;
                }
                else if (index >= 9)
                {
                    zboots = 1.02f;
                    xboots = 0.445f + (index - 9) * 0.27f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;

                }
                else
                {
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalx, scaly, scalz)
                       .Values;
                }


                
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