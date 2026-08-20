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

namespace MoreInventorys.src.BlockEntityFolder
{
    public class BEWardRobe : BlockEntityDisplay
    {
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "wardrobe";

        public override string AttributeTransformCode => "onwardrobeTransform";
        public override int DisplayedItems => slotCount;
        Block block;

        static int slotCount = 52;
        bool isOpen;

        private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;
        private bool _isOpen = false;

        public BEWardRobe()
        {
            inv = new InventoryGeneric(slotCount, "wardrobe-0", null);
        }

        public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);



            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                if (capi == null) return;

                AssetLocation shapeLoc = new AssetLocation("moreinventorys:shapes/wardrobe.json");
                Shape shape = Shape.TryGet(capi, shapeLoc);
                if (shape == null) return;

                animUtil?.InitializeAnimator("wardrobe", shape, null, new Vec3f(0, block.Shape.rotateY, 0));
            }
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            var sbi = blockSel.SelectionBoxIndex;

            if(!isOpen && sbi > 17)
            {
                OpenAnimation();
                isOpen = true;
            }
            else if (sbi > 17 && slot.Empty)
            {
                CloseAnimation();
                isOpen = false;
            }

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
            if (blockSel.SelectionBoxIndex < 12)
            {
                if (code.StartsWith("clothes") || code.StartsWith("armor-body") || code.StartsWith("armor-legs") || code.StartsWith("armor")) isValidTag = true;
                if ((code.Contains("shoulder") || code.Contains("upperbody") || code.StartsWith("armor-body") || code.StartsWith("armor-legs") || code.Contains("lowerbody")) && isValidTag)
                {
                    return true;
                }
            }
            else
            {
                if (code.Contains("foot") || code.Contains("head") || code.Contains("face") || code.Contains("hand") || code.Contains("bracers") || code.Contains("manacles"))
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
            int legsStartIndex = 40;
            if (blockSel.SelectionBoxIndex < 12)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 0:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 1:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 2:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 3:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 4:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 5:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 6:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 7:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 8:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 9:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 10:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;
                    case 11:
                        if (!inv[blockSel.SelectionBoxIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        else if (!inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty) return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + legsStartIndex);
                        break;

                    default:
                        break;
                }

            }
            else if (blockSel.SelectionBoxIndex >= 12)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 12:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 13:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 14:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 15:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 16:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 17:
                        if (!inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex);
                        }
                        else if (!inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return TakeBootsSlot(byPlayer, blockSel.SelectionBoxIndex + 6);
                        }
                        break;

                    default:
                        break;
                }
            }
            return false;
        }
        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {   //одежда верхние 12 слотов, далее 12-25 это ботинки/шлем/перчи/пояс/украшения
            var code = slot.Itemstack?.Item?.Code?.Path;
            if (code == null) return false;

            bool isChest = false;
            if (code.Contains("armor-body") || code.Contains("upperbody") || (code.Contains("shoulder"))) isChest = true;
            bool isLegs = false;
            if (code.Contains("armor-legs") || code.Contains("lowerbody")) isLegs = true;
            bool isFoot = false;
            if (code.Contains("foot")) isFoot = true;
            bool isHelmet = false;
            bool isHand = false;
            if (code.Contains("clothes") || code.Contains("armor"))
            {
                if (code.Contains("head") || code.Contains("face") || code.Contains("hand") || code.Contains("bracers") || code.Contains("manacles"))
                {
                    isHelmet = true;
                    isFoot = true;
                    isHand = true;
                }

            }

            int legsStartIndex = 40;

            if (blockSel.SelectionBoxIndex < 12)
            {
                switch (blockSel.SelectionBoxIndex)
                {
                    case 0:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 1:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 2:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 3:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 4:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 5:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 6:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 7:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 8:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 9:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 10:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    case 11:
                        if (inv[blockSel.SelectionBoxIndex].Empty && isChest) return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        else if (inv[blockSel.SelectionBoxIndex + legsStartIndex].Empty && isLegs)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + legsStartIndex);
                        }
                        break;
                    default:
                        break;
                }

            }
            else if (blockSel.SelectionBoxIndex >= 12 && blockSel.SelectionBoxIndex < 18)
            {
                if (!isFoot) return false;

                switch (blockSel.SelectionBoxIndex)
                {   //ботинки
                    case 12:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 13:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 14:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 15:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 16:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + 6);
                        }
                        break;
                    case 17:
                        if (inv[blockSel.SelectionBoxIndex].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex);
                        }
                        else if (inv[blockSel.SelectionBoxIndex + 6].Empty)
                        {
                            return PutClothsSlot(slot, blockSel.SelectionBoxIndex + 6);
                        }
                        break;

                    default:
                        break;
                }
            }
            return false;
        }

        bool PutClothsSlot(ItemSlot slot, int blockSelIndex)
        {
            if (inv[blockSelIndex].Empty)
            {
                int num = slot.TryPutInto(Api.World, inv[blockSelIndex]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }

            return false;
        }

        public void OpenAnimation()
        {
            if (animUtil == null) return;
            MarkDirty(true);
            Api.World.PlaySoundAt(new AssetLocation("sounds/block/chestopen"), Pos.X, Pos.Y, Pos.Z);

            animUtil.StartAnimation(new AnimationMetaData()
            {
                Animation = "open",
                Code = "open",
                AnimationSpeed = 3.0f,
                EaseOutSpeed = 12,
                EaseInSpeed = 30
            });
        }

        public void CloseAnimation()
        {
            if (animUtil == null) return;
            MarkDirty(true);
            Api.World.PlaySoundAt(new AssetLocation("sounds/block/chestclose"), Pos.X, Pos.Y, Pos.Z);
            animUtil.StopAnimation("open");
            animUtil.StartAnimation(new AnimationMetaData()
            {
                Animation = "close",
                Code = "close",
                AnimationSpeed = 3.0f,
                EaseOutSpeed = 12,
                EaseInSpeed = 30
            });
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            _isOpen = tree.GetBool("isOpen", false);
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
            tree.SetBool("isOpen", _isOpen);
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
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];

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
                float x = index * 0.125f + 0.65f;
                float z = 0.401f;
                float y = 0.93f;

                float yboots = 0.29f;
                float zboots = 0f;
                float xboots = 0f;


                //-----------ботинки-----------//
                
                if (index >= 12 && index < 18)
                {//передний ряд
                    if(code.Contains("foot"))
                    {
                        zboots = 1.3f;
                        xboots = 0.532f + (index - 12) * 0.265f;
                        yboots = 1.065f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;
                    }
                    else if(code.Contains("head") || code.Contains("face"))
                    {
                        if (code.Contains("head-barber") || code.Contains("head-miner") || code.Contains("head-musician")
                            || code.Contains("head-shepherd") || code.Contains("head-tailor") || code.Contains("head-alchemist"))
                        {
                            zboots = 1.3f;
                            xboots = 0.532f + (index - 12) * 0.265f;
                            yboots = 0.16f;
                        }
                        else if(code.Contains("clothes-nadiya-head"))
                        {
                            zboots = 1.3f;
                            xboots = 0.532f + (index - 12) * 0.265f;
                            yboots = 0.24f;
                        }
                        else if (code.Contains("face"))
                        {
                            zboots = 1.3f;
                            xboots = 0.532f + (index - 12) * 0.265f;
                            yboots = 0.34f;
                        }
                        else
                        {
                            zboots = 1.3f;
                            xboots = 0.532f + (index - 12) * 0.265f;
                            yboots = 0.285f;
                        }

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;

                    }
                    else if (code.Contains("hand") || code.Contains("bracers") || code.Contains("manacles"))
                    {
                        zboots = 0.7f;
                        xboots = 0.665f + (index - 12) * 0.265f;
                        yboots = 0.839f;
                        
                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.3f, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .RotateZDeg(90f)
                       .Values;
                    }
                    
                }
                else if (index >= 18 && index < 24)
                {//задний ряд
                    if(code.Contains("foot"))
                    {
                        zboots = 1.02f;
                        xboots = 0.532f + (index - 18) * 0.265f;
                        yboots = 1.065f;
                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;
                    }
                    else if (code.Contains("head") || code.Contains("face"))
                    {
                        if (code.Contains("head-barber") || code.Contains("head-miner") || code.Contains("head-musician")
                           || code.Contains("head-shepherd") || code.Contains("head-tailor") || code.Contains("head-alchemist"))
                        {
                            zboots = 1.02f;
                            xboots = 0.532f + (index - 18) * 0.265f;
                            yboots = 0.155f;
                        }
                        else if (code.Contains("clothes-nadiya-head"))
                        {
                            zboots = 1.02f;
                            xboots = 0.532f + (index - 18) * 0.265f;
                            yboots = 0.24f;
                        }
                        else if (code.Contains("face"))
                        {
                            zboots = 1.02f;
                            xboots = 0.532f + (index - 18) * 0.265f;
                            yboots = 0.34f;
                        }
                        else
                        {
                            zboots = 1.02f;
                            xboots = 0.532f + (index - 18) * 0.265f;
                            yboots = 0.285f;
                        }
                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;
                    }
                    else if (code.Contains("hand") || code.Contains("bracers") || code.Contains("manacles"))
                    {
                        zboots = 0.42f;
                        xboots = 0.665f + (index - 18) * 0.265f;
                        yboots = 0.839f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.3f, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .RotateZDeg(90f)
                       .Values;
                    }
                    

                }
                else if (index >=24 && index < 32)
                {//за дверцей передний ряд
                    zboots = 1.3f;
                    xboots = 0.445f + (index - 6) * 0.27f;
                    yboots = 0.2f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;

                }
                else if (index >= 32 && index < 40)
                {//за дверцей задний ряд
                    zboots = 1.02f;
                    xboots = 0.445f + (index - 9) * 0.27f;
                    yboots = 0.2f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(xboots - 0.5f, yboots, zboots - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalxBoots, scalyBoots, scalzBoots)
                       .RotateYDeg(90f)
                       .Values;

                }
                else if (index >= 40)
                {//верхние 12 слотов для штанов
                    x = 0.582f + (index - 39) * 0.125f;
                    y = 1.33f;

                    if (code.Contains("clothes-nadiya-lowerbody-fisher"))
                    {
                        y = 0.92f;
                    }
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scalx - 0.1f, scaly, scalz)
                       .Values;
                }
                else
                {//верхние 12 слотов для верхней одежды
                    if (code != null)
                    {
                        string[] shoulderAndUpperBodys =
                         {
                            "shoulder-survivor",
                            "shoulder-miner",
                            "shoulder-malefactor-cloak",
                            "shoulder-marketeer",
                            "ruralhunter",
                            "shoulder-stained-leather",
                            "upperbodyover-embroid",
                            "upperbodyover-arcticfisher",
                            "upperbodyover-arctichunter",
                            "upperbodyover-forgotten",
                            "upperbodyover-fur-coat",
                            "shoulder-midnight",
                            "shoulder-musician"
                        };

                        if (shoulderAndUpperBodys.Any(p => code.Contains(p)))
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



            }

            return tfMatrices;
        }
    }
}