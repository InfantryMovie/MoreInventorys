using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BESmallHorizontleSwordStand : BlockEntityDisplay
    {
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "smallHorizontleWeaponStandInventory";
        public override string AttributeTransformCode => "onSmallHorizontleWeaponStandInventoryTransform";

        Block block;

        static int slotCount = 5;

        public BESmallHorizontleSwordStand()
        {
            inv = new InventoryGeneric(slotCount, "smallHorizontleWeaponStand-0", null);
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

            if (!IsSword(slot.Itemstack)) return false; //только для оружия 

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            CollectibleObject colObj = slot.Itemstack.Collectible;

            //AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            
            if (TryPut(slot, blockSel))
            {
                //Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);
                MarkDirty();
                return true;
            }
            return false;
        }

        private bool IsSword(ItemStack itemstack)
        {
            if (itemstack == null) return false;

            if (itemstack.Collectible.Tool == null) return false;
            if (itemstack.Collectible.Code.Path.Contains("blade-sabre-ruined")) return false;
            if (itemstack.Collectible.Code.Path.Contains("blade") || itemstack.Collectible.Tool == EnumTool.Sword) return true;

            return false;

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
                float x = 0f; //Лева/Право
                float z = 0f; //Глубина
                float y = 0f; //Высота

                var weapon = inv[index];
                var code = weapon.Itemstack?.Item?.Code.ToString();
                //float x = (index * 0.15f расстояние между хитбоксами) + 0.65f начальное положение первого хитбокса

                if (string.IsNullOrEmpty(code))
                {
                    tfMatrices[index] = new Matrixf()
                    .Scale(0f, 0f, 0f)
                    .Values;
                    continue;
                }
                ;

                if(code.Contains("flax"))
                {
                    x = 1.7f;
                    z = 1.38f - (index * 0.11f); //глубина
                    y = 0.45f + (index * 0.08f); //высота
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 0.85f, 0.85f)
                       //.RotateZDeg(90f) // поднимает  вертикально
                       //.RotateYDeg(5f) // наклон 
                       .RotateYDeg(180f)
                       .RotateXDeg(50f)
                       .Values;
                } 
                else if (code.Contains("blade-arming-ruined") )
                {
                    x = 1.9f;
                    z = 1.38f - (index * 0.11f); //глубина
                    y = 0.52f + (index * 0.08f); //высота
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 0.85f, 0.85f)
                       //.RotateZDeg(90f) // поднимает  вертикально
                       //.RotateYDeg(5f) // наклон 
                       .RotateYDeg(180f)
                       .RotateXDeg(50f)
                       .Values;
                }
                else if (code.Contains("blade-scrap-scrap") || code.Contains("blade-forlorn"))
                {
                    x = 1.9f;
                    z = 1.36f - (index * 0.11f); //глубина
                    y = 0.5f + (index * 0.08f); //высота
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 0.85f, 0.85f)
                       //.RotateZDeg(90f) // поднимает  вертикально
                       //.RotateYDeg(5f) // наклон 
                       .RotateYDeg(180f)
                       .RotateXDeg(50f)
                       .Values;
                }
                else if (code.Contains("blade-gladius-ruined"))
                {
                    x = 1.9f;
                    z = 1.36f - (index * 0.11f); //глубина
                    y = 0.5f + (index * 0.08f); //высота
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 0.85f, 0.85f)
                       //.RotateZDeg(90f) // поднимает  вертикально
                       //.RotateYDeg(5f) // наклон 
                       .RotateYDeg(180f)
                       .RotateXDeg(50f)
                       .Values;
                }
                else if (code.Contains("blade-claymore-ruined"))
                {
                    x = 2.18f;
                    z = 1.36f - (index * 0.11f); //глубина
                    y = 0.5f + (index * 0.08f); //высота
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 0.85f, 0.85f)
                       //.RotateZDeg(90f) // поднимает  вертикально
                       //.RotateYDeg(5f) // наклон 
                       .RotateYDeg(180f)
                       .RotateXDeg(50f)
                       .Values;
                }
                else
                {
                    x = 1.7f;
                    z = 1.38f - (index * 0.11f); //глубина
                    y = 0.45f + (index * 0.08f); //высота
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(block.Shape.rotateY) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 0.85f, 0.85f)
                       //.RotateZDeg(90f) // поднимает  вертикально
                       //.RotateYDeg(5f) // наклон 
                       .RotateYDeg(180f)
                       .RotateXDeg(50f)
                       .Values;
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
