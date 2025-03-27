using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MoreInventorys.src
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

            if (!IsWeapon(slot.Itemstack)) return false; //только для оружия

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            CollectibleObject colObj = slot.Itemstack.Collectible;

            AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(slot, blockSel))
            {
                Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                MarkDirty();
                return true;
            }
            return false;
        }

        private bool IsWeapon(ItemStack itemstack)
        {
            if (itemstack == null) return false;
            string type = itemstack.Collectible.Code.Path;
            return type.Contains("spear"); 
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
                    Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
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
                float x = (index * 0.15f) + 0.67f;

                float y = 0.55f; //высота 1.01f
                float z = 0.2f; //глубина?

                tfMatrices[index] = new Matrixf()
                    .Translate(0.5f, 0f, 0.5f)
                    .RotateYDeg(block.Shape.rotateY)
                    .Translate(x - 0.5f, y, z - 0.4f)
                    .Translate(-0.5f, 0f, -0.5f)
                    .RotateZDeg(90f) // поднимает копья вертикально
                    .RotateYDeg(5f) // наклон для копей 
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
            for (int j = 3; j >= 0; j--)
            {
                int i = j + (up ? 4 : 0);
                i ^= 2;
                if (!inv[i].Empty)
                {
                    ItemStack stack = inv[i].Itemstack;
                    sb.AppendLine(stack.GetName());
                }
            }
        }
    }
}
