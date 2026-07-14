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
        const int MAX_WEAPON_SLOTS = 6;

        Dictionary<int, string> storageWeapons { get; set; }
        string weapon1;
        string weapon2;
        string weapon3;
        string weapon4;
        string weapon5;
        string weapon6;

        bool isFirstLoad;
        public BESmallVerticalWeaponstand()
        {
            inv = new InventoryGeneric(slotCount, "smallVerticalWeaponStand-0", null);
            storageWeapons = new Dictionary<int, string>();
            isFirstLoad = true;
        }

        public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);
        }

        bool InitializeStorageWeapons()
        {
            if (storageWeapons.Count > 0) return false;

            for (int i = 0; i < MAX_WEAPON_SLOTS; i++)
            {
                switch (i)
                {
                    case 0:
                        if (weapon1 != "") storageWeapons.Add(0, weapon1);
                        break;

                    case 1:
                        if (weapon2 != "") storageWeapons.Add(1, weapon2);
                        break;

                    case 2:
                        if (weapon3 != "") storageWeapons.Add(2, weapon3);
                        break;

                    case 3:
                        if (weapon4 != "") storageWeapons.Add(3, weapon4);
                        break;

                    case 4:
                        if (weapon5 != "") storageWeapons.Add(4, weapon5);
                        break;

                    case 5:
                        if (weapon6 != "") storageWeapons.Add(5, weapon6);
                        break;
                    default:
                        break;
                }
            }
            return true;
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

            var storageWeapon = slot.Itemstack.Item;

            //AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(slot, blockSel))
            {
                if (storageWeapon.Code.Path != "" && storageWeapons.Count != MAX_WEAPON_SLOTS)
                {
                    if(!storageWeapons.ContainsKey(blockSel.SelectionBoxIndex))
                    {
                        storageWeapons.Add(blockSel.SelectionBoxIndex, storageWeapon.Code.Path + DateTime.Now.ToString());
                    }
                    
                }

                switch (blockSel.SelectionBoxIndex)
                {
                    case 0:
                        weapon1 = storageWeapon.Code.Path;
                        break;

                    case 1:
                        weapon2 = storageWeapon.Code.Path;
                        break;

                    case 2:
                        weapon3 = storageWeapon.Code.Path;
                        break;

                    case 3:
                        weapon4 = storageWeapon.Code.Path;
                        break;

                    case 4:
                        weapon5 = storageWeapon.Code.Path;
                        break;

                    case 5:
                        weapon6 = storageWeapon.Code.Path;
                        break;

                    default:
                        break;
                }


                //Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
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
            //получаем значение до знака "-"
            int indexOfDash = input.IndexOf('-');

            if (indexOfDash >= 0)
            {
                return input.Substring(0, indexOfDash);
            }

            return input;
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
                    //AssetLocation sound = stack.Block?.Sounds?.Place;
                    //Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                    MoreInventorysMod.PlaySoundBlockAt(Api, stack, byPlayer);
                }
                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);



                if (storageWeapons.ContainsKey(blockSel.SelectionBoxIndex))
                {
                    storageWeapons.Remove(blockSel.SelectionBoxIndex);
                }

                switch (blockSel.SelectionBoxIndex)
                {
                    case 0:
                        weapon1 = string.Empty;
                        break;

                    case 1:
                        weapon2 = string.Empty;
                        break;

                    case 2:
                        weapon3 = string.Empty;
                        break;

                    case 3:
                        weapon4 = string.Empty;
                        break;

                    case 4:
                        weapon5 = string.Empty;
                        break;

                    case 5:
                        weapon6 = string.Empty;
                        break;

                    default:
                        break;
                }

                MarkDirty();
                return true;
            }
            return false;
        }

        (int, string) GetOrientationRateForMartices(int containerIndex)
        {
            int orientationRotate = 0;

            if (storageWeapons.Count == 0)
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (!storageWeapons.ContainsKey(containerIndex)) return (orientationRotate, "");

            var weapon = storageWeapons[containerIndex];
            if (string.IsNullOrEmpty(weapon))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (weapon.Contains("axe") && !weapon.Contains("pickaxe") && !weapon.Contains("prospectingpick"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("pickaxe"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "pickaxe");
            }
            if (weapon.Contains("prospectingpick"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "prospectingpick");
            }
            if (weapon.Contains("shears"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "shears");
            }
            if (weapon.Contains("knife") && !weapon.Contains("knife-dagger-ruined") && !weapon.Contains("knife-khanjar-ruined")
                && !weapon.Contains("knife-baselard-ruined") && !weapon.Contains("knife-stiletto-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "knife");
            }
            if (weapon.Contains("knife-dagger-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "knife-dagger-ruined");
            }
            if (weapon.Contains("knife-khanjar-ruined")) 
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "knife-khanjar-ruined");
            }
            if (weapon.Contains("knife-baselard-ruined")) 
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "knife-baselard-ruined");
            }
            if (weapon.Contains("knife-stiletto-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "knife-stiletto-ruined");
            }
            if (weapon.Contains("bugnet"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "bugnet");
            }
            if (weapon.Contains("spear"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade") && !weapon.Contains("blade-forlonr") && !weapon.Contains("blade-longsword-admin")
                && !weapon.Contains("blade-gladius-ruined") && !weapon.Contains("blade-arming-ruined") && !weapon.Contains("blade-claymore-ruined")
                && !weapon.Contains("blade-sabre-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade-sabre-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade-claymore-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade-arming-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade-forlonr"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade-longsword-admin"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("shovel"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("firestarter"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("hoe"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("cleaver"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "cleaver");
            }
            if (weapon.Contains("club") && !weapon.Contains("club-generic-wood"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "club");
            }
            if (weapon.Contains("club-generic-wood"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("oar"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "oar");
            }
            if (weapon.Contains("hammer"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "hammer");
            }
            if (weapon.Contains("saw"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "saw");
            }
            if (weapon.Contains("chisel"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "chisel");
            }
            if (weapon.Contains("wrench"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "wrench");
            }
            if (weapon.Contains("scythe"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "scythe");
            }
            if (weapon.Contains("sling"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "sling");
            }
            if (weapon.Contains("plumband"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "plumband");
            }
            if (weapon.Contains("bow"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("tongs"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("soldering"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            if (weapon.Contains("blade-gladius-ruined"))
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, weapon);
            }
            else
            {

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
            }

            return (orientationRotate, "");

        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];
            int orientationRotate = 0;
            string code = "";
            string[] hoesMetall = {"hoe-copper", "hoe-gold", "hoe-silver", "hoe-iron", "hoe-steel", "hoe-tinbronze", "hoe-bismuthbronze", 
                "hoe-blackbronze", "hoe-meteoriciron" };
            for (int index = 0; index < slotCount; index++)
            {
                var orientationRotateResult = GetOrientationRateForMartices(index);
                orientationRotate = orientationRotateResult.Item1;
                code = orientationRotateResult.Item2;


                //float x = (index * 0.15f расстояние между хитбоксами) + 0.65f начальное положение первого хитбокса
                float x = index * 0.15f + 0.67f;
                float z = 0.35f; //глубина
                float itemHeight = 0.1f;
                float y = itemHeight + 0.55f; //высота

                if (code.Contains("shovel"))
                {
                    y -= 0.20f;
                    z -= 0.11f;
                }

                

                /*if (code.Contains("hoe"))
                {
                    y -= 0.20f;
                    z -= 0.095f;
                }*/
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

                    if(!anyMatch)
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
                   .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                   .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                   .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                   .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                   .Scale(1f, 1f, 1f)
                   .RotateZDeg(90f) // поднимает  вертикально
                   .RotateYDeg(180f) // наклон 
                   .Values;
                }
                else if (code.Contains("bow"))
                {
                    y -= 0.29f;
                    z -= 0.02f;
                    x -= 0.05f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(15f) // наклон 
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
                        y += 0.35f;
                        z -= 0.11f;
                        x -= 0.013f;
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
                        code.Contains("spear-generic-steel") || code.Contains("spear-generic-blackbronze") || code.Contains("spear-generic-bismuthbronze"))
                    {
                        y += 0.4f;
                        z -= 0.11f;
                        x += 0f;
                    }
                    else
                    {
                        y -= 0.05f;
                        z -= 0.11f;
                        
                    }

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                      .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                      .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                      .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f) // поднимает  вертикально
                      .RotateYDeg(3f) // наклон 
                      .Values;

                }
                else if (code.Contains("saw"))
                {
                    y = 0.1f;
                    z = 0.18f;
                    x += 0.425f;

                    tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                   .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                   .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                   .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                   .Scale(1f, 1f, 1f)
                   .RotateZDeg(90f) // поднимает  вертикально
                   .RotateYDeg(360f) // наклон 
                   .Values;
                }
                else if (code.Contains("knife") && !code.Contains("knife-dagger-ruined") && !code.Contains("knife-khanjar-ruined")
                    && !code.Contains("knife-baselard-ruined") && !code.Contains("knife-stiletto-ruined"))
                {
                    y -= 0.35f;
                    z -= 0.11f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(5f) // наклон 
                       .Values;
                }
                else if (code.Contains("knife-stiletto-ruined"))
                {
                    y -= 0.35f;
                    z -= 0.11f;
                    x -= 0.05f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(5f) // наклон 
                       .Values;
                }
                else if (code.Contains("knife-dagger-ruined")) 
                {
                    y += 0.45f;
                    z += 0.493f;
                    x -= 0.5f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(205f) // наклон 
                       .RotateXDeg(65f)
                       .Values;
                }
                else if (code.Contains("knife-khanjar-ruined")) 
                {
                    y += 0.33f;
                    z += 0.25f;
                    x -= 0.55f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(215f) // наклон 
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("knife-baselard-ruined")) 
                {
                    y += 0.43f;
                    z += 0.05f;
                    x -= 0.5f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(200f) // наклон 
                       .RotateXDeg(115f)
                       .Values;
                }
                else if (code.Contains("wrench"))
                {
                    y -= 0.78f;
                    z -= 0.1f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(0f) // наклон 
                       .Values;

                }
                else if (code.Contains("scythe"))
                {
                    y -= 0.10f;
                    z -= 0.01f;
                    x -= 0.01f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(5f) // наклон 
                       .Values;
                }
                else if (code.Contains("cleaver"))
                {
                    y -= 0.75f;
                    z -= 0.12f;
                    x -= 0.545f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f,1f,1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(3f) // наклон 
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("club") && !code.Contains("club-generic-wood")) 
                {
                    y -= 0.65f;
                    z += 0.4f;
                    x -= 0.445f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(2f) // наклон 
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("club-generic-wood")) 
                {
                    y -= 0.65f;
                    z += 0.02f;
                    x -= 0.445f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(2f) // наклон 
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("plumband"))
                {
                    y += 0.18f;
                    z += 0.77f;
                    x -= 0.445f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(135f) // наклон 
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("firestarter"))
                {
                    y -= 0.65f; // высота
                    z += 0.4f; // глубина
                    x -= 0.05f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.75f, 0.75f, 0.75f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(-45f) // наклон 
                       .RotateXDeg(90f)
                       .Values;
                }
                else if (code.Contains("bugnet"))
                {
                    y -= 0.38f;
                    z += 0.41f;
                    x -= 0.56f;
                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                      .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                      .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                      .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f) // поднимает  вертикально
                      .RotateYDeg(3f) // наклон 
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
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;
                    }
                    else if (code.Contains("blade-forlorn"))
                    {
                        y = 1.75f;
                        z = 0.95f;
                        x -= 0.05f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.9f, 0.9f, 0.9f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;
                    }
                    else if (code.Contains("blade-longsword-admin"))
                    {
                        y = 1.48f;
                        z = 1.05f;
                        x += 0.01f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(0.9f, 0.9f, 0.9f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;
                    }
                    else if (code.Contains("blade-gladius-ruined"))
                    {
                        y = 1.84f;
                        z = 1.01f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;
                    }
                    else if (code.Contains("blade-arming-ruined"))
                    {
                        y = 2.44f;
                        z = 0.93f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;
                    }
                    else if (code.Contains("blade-claymore-ruined"))
                    {
                        y = 3.1f;
                        z = 0.67f;
                        x -= 0.045f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;
                    }
                    else if (code.Contains("blade-sabre-ruined"))
                    {
                        y = 2.1f;
                        z = 0.7f;
                        x -= 0.067f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(215f) // наклон 
                       .Values;
                    }
                    else
                    {
                        y = 1.27f;
                        z = 1.15f;
                        x += 0.001f;

                        tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(195f) // наклон 
                       .Values;

                    }
                }
                else if (code.Contains("oar"))
                {
                    y = 0.64f;
                    z = 1.13f;
                    x -= 0.38f;

                    tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                   .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                   .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                   .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                   .Scale(1f, 1f, 1f)
                   .RotateZDeg(90f) // поднимает  вертикально
                   .RotateYDeg(3f) // наклон 
                   .RotateXDeg(140f)
                   .Values;
                }
                else if (code.Contains("axe") && !code.Contains("pickaxe") && !code.Contains("prospectingpick"))
                {
                    if(code.Contains("axe-battle"))
                    {
                        y -= 0.42f;
                        z -= 0.09f;
                        x -= 0.05f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 
                          .Values;
                    }
                    else if (code.Contains("axe-double"))
                    {
                        y -= 0.17f;
                        z -= 0.09f;
                        x += 0.08f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 
                          .RotateXDeg(-15f)
                          .Values;
                    }
                    else if (code.Contains("axe-bearded"))
                    {
                        y -= 0.55f;
                        z -= 0.09f;
                        x -= 0.05f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 

                          .Values;
                    }
                    else if (code.Contains("axe-scrap"))
                    {
                        y -= 0.59f;
                        z -= 0.115f;
                        x -= 0.045f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 

                          .Values;
                    }
                    else if (code.Contains("axe-bardiche"))
                    {
                        y += 0.11f;
                        z -= 0.13f;
                        x -= 0.045f;
                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 

                          .Values;
                    }
                    else if (code.Contains("axe-bone"))
                    {
                        y -= 0.45f;
                        z -= 0.095f;

                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 
                          .Values;
                    }
                    else
                    {
                        y -= 0.55f;
                        z -= 0.11f;

                        tfMatrices[index] = new Matrixf()
                          .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                          .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                          .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                          .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                          .Scale(1f, 1f, 1f)
                          .RotateZDeg(90f) // поднимает  вертикально
                          .RotateYDeg(3f) // наклон 
                          .Values;

                    }


                }
                else if (code.Contains("pickaxe"))
                {
                    y -= 0.62f;
                    z -= 0.09f;

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                      .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                      .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                      .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f) // поднимает  вертикально
                      .RotateYDeg(3f) // наклон 
                      .Values;
                }
                else if (code.Contains("prospectingpick"))
                {
                    y -= 0.62f;
                    z -= 0.09f;

                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                      .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                      .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                      .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f) // поднимает  вертикально
                      .RotateYDeg(3f) // наклон 
                      .Values;
                }
                else if (code.Contains("sling"))
                {
                    y -= 0.20f;
                    z += 0.355f;
                    x -= 0.55f;
                    tfMatrices[index] = new Matrixf()
                      .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                      .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                      .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                      .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                      .Scale(1f, 1f, 1f)
                      .RotateZDeg(90f) // поднимает  вертикально
                      .RotateYDeg(5f) // наклон 
                      .RotateXDeg(90f)
                      .Values;
                }
                else if (code.Contains("tongs"))
                {
                    y += 0.62f;
                    z += 0.8f;
                    x += 0.45f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(180f) // наклон 
                       .Values;
                }
                else
                {
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f) // Сначала перемещаем предмет в центр блока
                       .RotateYDeg(orientationRotate) // Поворачиваем предмет по оси Y (если сам блок повернут)
                       .Translate(x - 0.5f, y, z - 0.4f) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(1f, 1f, 1f)
                       .RotateZDeg(90f) // поднимает  вертикально
                       .RotateYDeg(5f) // наклон 
                       .Values;

                }

                
            }

            return tfMatrices;
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            weapon1 = tree.GetString("weapon1");
            weapon2 = tree.GetString("weapon2");
            weapon3 = tree.GetString("weapon3");
            weapon4 = tree.GetString("weapon4");
            weapon5 = tree.GetString("weapon5");
            weapon6 = tree.GetString("weapon6");

            if (isFirstLoad)
            {
                isFirstLoad = false;
                bool num = InitializeStorageWeapons();
            }

            RedrawAfterReceivingTreeAttributes(worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("weapon1", weapon1);
            tree.SetString("weapon2", weapon2);
            tree.SetString("weapon3", weapon3);
            tree.SetString("weapon4", weapon4);
            tree.SetString("weapon5", weapon5);
            tree.SetString("weapon6", weapon6);
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
