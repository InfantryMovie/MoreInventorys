using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BEShieldStand : BlockEntityDisplay
    {
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "shieldstand";

        public override string AttributeTransformCode => "onShieldStandInventoryTransform";

        Block block;

        static int slotCount = 3;

        public BEShieldStand()
        {
            inv = new InventoryGeneric(slotCount, "shieldStand-0", null);
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

            

            if (!IsSwordOrShield(slot.Itemstack)) return false;

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            CollectibleObject colObj = slot.Itemstack.Collectible;

            //AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);


            if (TryPut(slot, blockSel))
            {
                //Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);
                MarkDirty();
                return true;
            }
            return false;
        }

        private bool IsSwordOrShield(ItemStack itemstack)
        {
            if (itemstack == null) return false;

            if (itemstack.Collectible.Code.Path.Contains("shield") || itemstack.Collectible.Code.Path.Contains("blade")) return true;

            return false;

        }

        private bool IsShield(ItemStack itemstack)
        {
            if (itemstack == null) return false;

            if (itemstack.Collectible.Code.Path.Contains("shield")) return true;

            return false;
        }



        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            // 0 - shield, 1 и 2 - sword
            if (inv[blockSel.SelectionBoxIndex].Empty)
            {
                if (blockSel.SelectionBoxIndex == 0 && !IsShield(slot.Itemstack)) return false;

                if (blockSel.SelectionBoxIndex == 1 && IsShield(slot.Itemstack)) return false;

                if (blockSel.SelectionBoxIndex == 2 && IsShield(slot.Itemstack)) return false;

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
                    //AssetLocation sound = stack.Block?.Sounds?.Place;
                    //Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
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



            for (int index = 0; index < slotCount; index++)
            {

                var invSlot = inv[index];
                var code = invSlot.Itemstack?.Item?.Code.ToString();
                if (code == null) continue;

                //float x = (index * 0.15f расстояние между хитбоксами) + 0.65f начальное положение первого хитбокса //float x = (index * 0.15f) + 0.67f;
                float x = 0f; //ширина
                float z = 0f; //глубина
                float y = 0f; //высота
                float scale = 0.7f;

                switch (index)
                {
                    //shield
                    case 0:
                        x = 1.0f;
                        z = 0.35f;
                        y = 0.45f;
                        tfMatrices[index] = new Matrixf()
                           .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                           .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                           .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                           .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                           .Scale(1f, 1f, 1f)
                           .RotateZDeg(45f)
                           .Values;
                        break;

                    //sword1
                    case 1:

                        if (code.Contains("falx"))
                        {
                            x = 0.29f;
                            z = 0.46f;
                            y = 0.25f;
                            tfMatrices[index] = new Matrixf()
                              .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(-91.6f)
                               .RotateXDeg(90f)
                               .RotateYDeg(155f)
                               .Values;
 
                        }
                        else if (code.Contains("blade-scrap-scrap")) {
                            x = 0.29f;
                            z = 0.48f;
                            y = 0.25f;
                            tfMatrices[index] = new Matrixf()
                              .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(-91.6f)
                               .RotateXDeg(90f)
                               .RotateYDeg(155f)
                               .Values;
                            
                        }
                        else if (code.Contains("blade-forlorn"))
                        {
                            x = 0.29f;
                            z = 0.5f;
                            y = 0.25f;
                            tfMatrices[index] = new Matrixf()
                              .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(-91.6f)
                               .RotateXDeg(90f)
                               .RotateYDeg(155f)
                               .Values;
                            /*
                             * 
                             * .RotateZDeg(235f)
                               .RotateXDeg(-90f)
                               .RotateY(90f)
                             * */

                        }
                        else
                        {
                            x = 0.29f;
                            z = 0.46f;
                            y = 0.25f;
                            tfMatrices[index] = new Matrixf()
                              .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(-91.6f)
                               .RotateXDeg(90f)
                               .RotateYDeg(155f)
                               .Values;
                        }

                        break;

                    //sword2
                    case 2:
                        if (code.Contains("falx"))
                        {
                            x = 1.73f;
                            z = 0.56f;
                            y = 0.27f;
                            tfMatrices[index] = new Matrixf()
                               .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(235f)
                               .RotateXDeg(-90f)
                               .RotateY(90f)
                               .Values;

                        }
                        else if (code.Contains("blade-scrap-scrap"))
                        {
                            x = 1.73f;
                            z = 0.49f;
                            y = 0.27f;
                            tfMatrices[index] = new Matrixf()
                               .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(235f)
                               .RotateXDeg(-90f)
                               .RotateY(90f)
                               .Values;

                        }
                        else if (code.Contains("blade-forlorn"))
                        {
                            x = 1.73f;
                            z = 0.50f;
                            y = 0.27f;
                            tfMatrices[index] = new Matrixf()
                               .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(235f)
                               .RotateXDeg(-90f)
                               .RotateY(90f)
                               .Values;

     

                        }

                        else
                        {
                            x = 1.73f;
                            z = 0.56f;
                            y = 0.27f;
                            tfMatrices[index] = new Matrixf()
                               .Translate(0.5f, 0f, 0.5f)
                               .RotateYDeg(block.Shape.rotateY)
                               .Translate(x - 0.5f, y, z - 0.4f)
                               .Translate(-0.5f, 0f, -0.5f)
                               .Scale(scale, scale, scale)
                               .RotateZDeg(235f)
                               .RotateXDeg(-90f)
                               .RotateY(90f)
                               .Values;
                        }

                        break;

                    default:
                        break;
                }


            }

            return tfMatrices;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RedrawAfterReceivingTreeAttributes(worldForResolving);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            sb.AppendLine();
            bool up = forPlayer.CurrentBlockSelection != null && forPlayer.CurrentBlockSelection.SelectionBoxIndex > 1;
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
