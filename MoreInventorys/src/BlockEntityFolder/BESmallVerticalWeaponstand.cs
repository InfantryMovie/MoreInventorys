using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using VintagestoryAPI.Math;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BESmallVerticalWeaponstand : BlockEntityDisplay
    {
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "smallVerticalWeaponStandInventory";
        public override string AttributeTransformCode => "onSmallVerticalWeaponStandInventoryTransform";

        Block block;

        static int slotCount = 6;

        public BESmallVerticalWeaponstand()
        {
            inv = new InventoryGeneric(slotCount, "smallVerticalWeaponStand-0", null);
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

            if (!IsTool(slot.Itemstack)) return false; //только для оружия 

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            CollectibleObject colObj = slot.Itemstack.Collectible;

            AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(slot, blockSel))
            {
                Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                MarkDirty();
                return true;
            }
            return false;
        }

        private bool IsTool(ItemStack itemstack)
        {
            if (itemstack == null) return false;
            if (itemstack.Collectible.Code.Path.Contains("saw")) return false;

            return itemstack.Collectible.Tool != null ? true : false;

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
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
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
                //float x = (index * 0.15f расстояние между хитбоксами) + 0.65f начальное положение первого хитбокса
                float x = index * 0.15f + 0.67f;
                float z = 0.35f; //глубина
                float itemHeight = 0.1f;

                ItemSlot slot = inv[index];

                if (!slot.Empty)
                {
                    CollectibleObject colObj = slot.Itemstack.Collectible;

                    if (colObj.Attributes.KeyExists("toolrackTransform"))
                    {
                        JsonObject toolrackTransform = colObj.Attributes["toolrackTransform"];

                        if (toolrackTransform != null && toolrackTransform.KeyExists("translation"))
                        {
                            JsonObject translation = toolrackTransform["translation"];

                            if (translation != null && translation.KeyExists("x"))
                            {
                                itemHeight = translation["x"].AsFloat(0.1f);
                            }
                        }
                    }
                }

                float y = itemHeight + 0.55f; //высота

                tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                   .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                   .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                   .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                   .Scale(0.75f, 0.75f, 0.75f)
                   .RotateZDeg(90f) // поднимает  вертикально
                   .RotateYDeg(5f) // наклон 
                   .Values;
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
