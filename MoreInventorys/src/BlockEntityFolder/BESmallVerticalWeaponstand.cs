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
using Vintagestory.ServerMods;

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

            if (!IsValidWeaponOrTool(slot)) return false;

            if (slot.Itemstack.Collectible.ItemClass != EnumItemClass.Item) return false;

            if (TryPut(slot, blockSel))
            {
                MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);
                MarkDirty();
                return true;
            }
            return false;
        }

        public bool IsValidWeaponOrTool(ItemSlot slot)
        {
            if (slot.Itemstack.Item == null) return false;

            string cod = GetValueBeforeDash(slot.Itemstack.Item?.Code?.Path);
            if (!ModConfigFile.Current.VanilaStorageWeaponsCode.Contains(cod) && !ModConfigFile.Current.ModedStorageWeaponsCode.ContainsKey(cod))
                return false;

            return true;
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

            string[] hoesMetall = {"hoe-copper", "hoe-gold", "hoe-silver", "hoe-iron", "hoe-steel", "hoe-tinbronze", "hoe-bismuthbronze",
                "hoe-blackbronze", "hoe-meteoriciron" };

            for (int index = 0; index < slotCount; index++)
            {
                var weapon = inv[index];
                var code = weapon.Itemstack?.Item?.Code?.Path;

                if (string.IsNullOrEmpty(code))
                {
                    tfMatrices[index] = new Matrixf()
                        .Scale(0f, 0f, 0f)
                        .Values;
                    continue;
                }

                float x = index * 0.15f + 0.67f;
                float z = 0.35f;
                float itemHeight = 0.1f;
                float y = itemHeight + 0.55f;

                if (code.Contains("shovel") && !code.Contains("snowshovel"))
                {
                    y -= 0.20f;
                    z -= 0.11f;
                }

                if (code.Contains("snowshovel"))
                {
                    y -= 0.37f;
                    z -= 0.11f;
                }

                if (code.Contains("fishingpole"))
                {
                    y -= 0.69f;
                    z -= 0.11f;
                }

                if (code.Contains("hoe"))
                {
                    bool anyMatch = false;
                    foreach (var hm in hoesMetall)
                    {
                        if (code.Contains(hm))
                        {
                            anyMatch = true;
                            y -= 0.20f;
                            z -= 0.095f;
                        }
                    }

                    if (!anyMatch)
                    {
                        y -= 0.55f;
                        z -= 0.095f;
                    }
                }

                if (code.Contains("hammer"))
                {
                    y -= 0.81f;
                    z -= 0.095f;
                    x += 0.454f;
                }

                if (code.Contains("chisel"))
                {
                    y -= 0.88f;
                    z -= 0.080f;
                    x -= 0.01f;
                }

                if (code.Contains("soldering"))
                {
                    y -= 0.88f;
                    z -= 0.080f;
                    x -= 0.01f;
                }

                if (code.Contains("shears"))
                {
                    y = 1.22f;
                    z = 1.15f;
                    x -= 0.020f;

                    tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f)
                   .RotateYDeg(orientationRotate)
                   .Translate(x - 0.5f, y, z - 0.4f)
                   .Translate(-0.5f, 0f, -0.5f)
                   .Scale(1f, 1f, 1f)
                   .RotateZDeg(90f)
                   .RotateYDeg(180f)
                   .Values;
                }
                else if (code.Contains("bow"))
                {
                    y -= 0.29f;
                    z -= 0.02f;
                    x -= 0.05f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(15f)
                       .Values;
                }
                else if (code.StartsWith("spear"))
                {
                    if (code.Contains("spear-generic-hacking"))
                    {
                        y -= 0.15f;
                        z -= 0.11f;
                        x -= 0.033f;
                    }
                    else if (code.Contains("spear-generic-ornate"))
                    {
                        y += 0.4f;
                        z += 0.32f;
                        x -= 0.55f;
                    }
                    else if (code.Contains("spear-scrap"))
                    {
                        y -= 0.15f;
                        z -= 0.11f;
                        x -= 0.031f;
                    }
                    else if (code.Contains("ruined") && !code.Contains("spear-fork-ruined"))
                    {
                        y += 0.35f;
                        z -= 0.11f;
                        x -= 0.046f;
                    }
                    else if (code.Contains("spear-fork-ruined"))
                    {
                        y += 0.84f;
                        z -= 0.11f;
                        x -= 0.046f;
                    }
                    else if (code.Contains("spear-generic-erel"))
                    {
                        y += 0.64f;
                        z -= 0.15f;
                        x -= 0.046f;
                    }
                    else if (code.Contains("spear-generic-copper") || code.Contains("spear-generic-iron") || code.Contains("spear-generic-meteoriciron") ||
                        code.Contains("spear-generic-steel") || code.Contains("spear-generic-blackbronze") || code.Contains("spear-generic-bismuthbronze") || code.Contains("spear-generic-tinbronze"))
                    {
                        y += 0.4f;
                        z += 0.32f;
                        x -= 0.55f;
                    }
                    else
                    {
                        y -= 0.05f;
                        z -= 0.11f;
                    }

                    if (code.Contains("spear-generic-copper") || code.Contains("spear-generic-iron") || code.Contains("spear-generic-meteoriciron") ||
                        code.Contains("spear-generic-steel") || code.Contains("spear-generic-blackbronze") || code.Contains("spear-generic-tinbronze") || code.Contains("spear-generic-bismuthbronze") || code.Contains("spear-generic-ornate"))
                    {
                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(3f)
                       .RotateXDeg(90f)
                       .Values;
                    }
                    else
                    {
                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(3f)
                       .Values;
                    }
                }
                else if (code.Contains("saw"))
                {
                    y = 0.1f;
                    z = 0.18f;
                    x += 0.425f;

                    tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f)
                   .RotateYDeg(orientationRotate)
                   .Translate(x - 0.5f, y, z - 0.4f)
                   .Translate(-0.5f, 0f, -0.5f)
                   .Scale(1f, 1f, 1f)
                   .RotateZDeg(90f)
                   .RotateYDeg(360f)
                   .Values;
                }
                else if (code.Contains("knife") && !code.Contains("knife-dagger-ruined") && !code.Contains("knife-khanjar-ruined")
                    && !code.Contains("knife-baselard-ruined") && !code.Contains("knife-stiletto-ruined"))
                {
                    y -= 0.35f;
                    z -= 0.11f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(5f)
                       .Values;
                }
                else if (code.Contains("knife-stiletto-ruined"))
                {
                    y -= 0.35f;
                    z -= 0.11f;
                    x -= 0.05f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(5f)
                       .Values;
                }
                else if (code.Contains("knife-dagger-ruined"))
                {
                    y += 0.45f;
                    z += 0.493f;
                    x -= 0.5f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(205f)
                       .RotateXDeg(65f)
                       .Values;
                }
                else if (code.Contains("knife-khanjar-ruined"))
                {
                    y += 0.33f;
                    z += 0.25f;
                    x -= 0.55f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(215f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("knife-baselard-ruined"))
                {
                    y += 0.43f;
                    z += 0.05f;
                    x -= 0.5f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(200f)
                       .RotateXDeg(115f)
                       .Values;
                }
                else if (code.Contains("wrench"))
                {
                    y -= 0.78f;
                    z -= 0.1f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(0f)
                       .Values;
                }
                else if (code.Contains("scythe"))
                {
                    y -= 0.10f;
                    z += 0.77f;
                    x -= 0.085f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f)
                       .RotateYDeg(5f)
                       .RotateXDeg(180f)
                       .Values;
                }
                else if (code.Contains("cleaver"))
                {
                    y -= 0.75f;
                    z -= 0.12f;
                    x -= 0.545f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(3f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("club") && !code.Contains("club-generic-wood") && !code.Contains("club-warhammer-ruined"))
                {
                    y -= 0.65f;
                    z += 0.4f;
                    x -= 0.445f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f)
                       .RotateYDeg(2f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("club-generic-wood"))
                {
                    y -= 0.65f;
                    z += 0.02f;
                    x -= 0.445f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f)
                       .RotateYDeg(2f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("club-warhammer-ruined"))
                {
                    y += 0.15f;
                    z += 0.47f;
                    x -= 0.87f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f)
                       .RotateYDeg(2f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("plumband"))
                {
                    y += 0.18f;
                    z += 0.77f;
                    x -= 0.445f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f)
                       .RotateYDeg(135f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("firestarter"))
                {
                    y -= 0.65f;
                    z += 0.4f;
                    x -= 0.05f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f)
                       .RotateYDeg(-45f)
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("bugnet"))
                {
                    y -= 0.38f;
                    z += 0.41f;
                    x -= 0.56f;
                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f)
                      .RotateYDeg(orientationRotate)
                      .Translate(x - 0.5f, y, z - 0.4f)
                      .Translate(-0.5f, 0f, -0.5f)
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f)
                      .RotateYDeg(3f)
                      .RotateXDeg(90f)
                      .Values;
                }
                else if (code.Contains("blade"))
                {
                    if (code.Contains("blade-scrap"))
                    {
                        y = 1.55f;
                        z = 1.06f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                    else if (code.Contains("blade-forlorn"))
                    {
                        y = 1.75f;
                        z = 0.95f;
                        x -= 0.05f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.9f, 0.9f, 0.9f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                    else if (code.Contains("blade-longsword-admin"))
                    {
                        y = 1.48f;
                        z = 1.05f;
                        x += 0.01f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(0.9f, 0.9f, 0.9f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                    else if (code.Contains("blade-gladius-ruined"))
                    {
                        y = 1.84f;
                        z = 1.01f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                    else if (code.Contains("blade-arming-ruined"))
                    {
                        y = 2.44f;
                        z = 0.93f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                    else if (code.Contains("blade-claymore-ruined"))
                    {
                        y = 3.1f;
                        z = 0.67f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                    else if (code.Contains("blade-sabre-ruined"))
                    {
                        y = 2.1f;
                        z = 0.7f;
                        x -= 0.067f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(215f)
                       .Values;
                    }
                    else
                    {
                        y = 1.27f;
                        z = 1.15f;
                        x += 0.001f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(195f)
                       .Values;
                    }
                }
                else if (code.Contains("oar"))
                {
                    y = 0.64f;
                    z = 1.13f;
                    x -= 0.38f;

                    tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f)
                   .RotateYDeg(orientationRotate)
                   .Translate(x - 0.5f, y, z - 0.4f)
                   .Translate(-0.5f, 0f, -0.5f)
                   .Scale(1f, 1f, 1f)
                   .RotateZDeg(90f)
                   .RotateYDeg(3f)
                   .RotateXDeg(140f)
                   .Values;
                }
                else if (code.Contains("axe") && !code.Contains("pickaxe") && !code.Contains("prospectingpick"))
                {
                    if (code.Contains("axe-battle"))
                    {
                        y -= 0.42f;
                        z -= 0.09f;
                        x -= 0.05f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .Values;
                    }
                    else if (code.Contains("axe-double"))
                    {
                        y -= 0.17f;
                        z -= 0.09f;
                        x += 0.08f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .RotateXDeg(-15f)
                          .Values;
                    }
                    else if (code.Contains("axe-bearded"))
                    {
                        y -= 0.55f;
                        z -= 0.09f;
                        x -= 0.05f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .Values;
                    }
                    else if (code.Contains("axe-scrap"))
                    {
                        y -= 0.59f;
                        z -= 0.115f;
                        x -= 0.045f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .Values;
                    }
                    else if (code.Contains("axe-bardiche"))
                    {
                        y += 0.11f;
                        z -= 0.13f;
                        x -= 0.045f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .Values;
                    }
                    else if (code.Contains("axe-bone"))
                    {
                        y -= 0.45f;
                        z -= 0.095f;

                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .Values;
                    }
                    else
                    {
                        y -= 0.55f;
                        z -= 0.11f;

                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f)
                          .RotateYDeg(orientationRotate)
                          .Translate(x - 0.5f, y, z - 0.4f)
                          .Translate(-0.5f, 0f, -0.5f)
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f)
                          .RotateYDeg(3f)
                          .Values;
                    }
                }
                else if (code.Contains("pickaxe"))
                {
                    y -= 0.62f;
                    z -= 0.09f;

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f)
                      .RotateYDeg(orientationRotate)
                      .Translate(x - 0.5f, y, z - 0.4f)
                      .Translate(-0.5f, 0f, -0.5f)
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f)
                      .RotateYDeg(3f)
                      .Values;
                }
                else if (code.Contains("crowbar-steel") || code.Contains("crowbar-iron") || code.Contains("crowbar-meteoriciron"))
                {
                    y -= 0.69f;
                    z += 0.37f;
                    x -= 0.54f;

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f)
                      .RotateYDeg(orientationRotate)
                      .Translate(x - 0.5f, y, z - 0.4f)
                      .Translate(-0.5f, 0f, -0.5f)
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f)
                      .RotateYDeg(5f)
                      .RotateXDeg(90f)
                      .Values;
                }
                else if (code.Contains("crowbar-copper") || code.Contains("crowbar-gold") || code.Contains("crowbar-silver")
                    || code.Contains("crowbar-tinbronze") || code.Contains("crowbar-bismuthbronze") || code.Contains("crowbar-blackbronze"))
                {
                    y -= 0.69f;
                    z += 0.45f;
                    x -= 0.54f;

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f)
                      .RotateYDeg(orientationRotate)
                      .Translate(x - 0.5f, y, z - 0.4f)
                      .Translate(-0.5f, 0f, -0.5f)
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f)
                      .RotateYDeg(5f)
                      .RotateXDeg(90f)
                      .Values;
                }
                else if (code.Contains("prospectingpick"))
                {
                    y -= 0.62f;
                    z -= 0.09f;

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f)
                      .RotateYDeg(orientationRotate)
                      .Translate(x - 0.5f, y, z - 0.4f)
                      .Translate(-0.5f, 0f, -0.5f)
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f)
                      .RotateYDeg(3f)
                      .Values;
                }
                else if (code.Contains("sling"))
                {
                    y -= 0.20f;
                    z += 0.355f;
                    x -= 0.55f;
                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f)
                      .RotateYDeg(orientationRotate)
                      .Translate(x - 0.5f, y, z - 0.4f)
                      .Translate(-0.5f, 0f, -0.5f)
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f)
                      .RotateYDeg(5f)
                      .RotateXDeg(90f)
                      .Values;
                }
                else if (code.Contains("tongs") && !code.Contains("tongsmetal"))
                {
                    y += 0.62f;
                    z += 0.8f;
                    x += 0.45f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(180f)
                       .Values;
                }
                else if (code.Contains("tongsmetal"))
                {
                    y += 0.62f;
                    z += 0.8f;
                    x -= 0.05f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(180f)
                       .Values;
                }
                else
                {
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 0.5f, y, z - 0.4f)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f)
                       .RotateYDeg(5f)
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