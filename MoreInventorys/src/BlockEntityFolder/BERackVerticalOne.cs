using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MoreInventorys.src.GuiFolder;
using MoreInventorys.src.InventoryFolder;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static System.Reflection.Metadata.BlobBuilder;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BERackVerticalOne : BlockEntityDisplay
    {
        Dictionary<int, int> containerSlotAddedSlots = new Dictionary<int, int>();
        Block block;
        InventoryDynamic inventory;
        
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "rackverticalonedynamic";

        GuiDialogDynamic storageDlg;

        //число слотов для инвентарей которые будут установлены на стеллажи
        public const int MAX_CONTAINER_BLOC_SLOTS = 3;
        public static IPlayer fromPlayer;


        public bool isOpened;

        public BERackVerticalOne()
        {
            inventory = new InventoryDynamic("rackverticalone-0", 3, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            inventory.SlotModified += OnSlotModified;
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);

        }

        private void OnSlotModified(int slotid)
        {


            if (Api.World.Side == EnumAppSide.Client)
            {
                UpdateShape();
                return;
            }
            
            UpdateShape();
        }

        public void UpdateShape()
        {
            MarkDirty(Api.Side != EnumAppSide.Server);
        }

       
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == 1101)
            {
                isOpened = BitConverter.ToBoolean(data, 0);
            }
            if (packetid == 1000)
            {
                using MemoryStream ms = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(ms);
                TreeAttribute tree = new TreeAttribute();
                tree.FromBytes(reader);
                Inventory.FromTreeAttributes(tree);
                Inventory.ResolveBlocksOrItems();
                _ = (IClientWorldAccessor)Api.World;
                if (storageDlg == null)
                {
                    Open();
                    storageDlg = new GuiDialogDynamic(inventory.dynamicSlots, Lang.Get("moreinventorys:rackverticalone-title"), (InventoryDynamic)Inventory, Pos, Api as ICoreClientAPI);
                    storageDlg.OnClosed += delegate
                    {
                        Open();
                        capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001);
                        storageDlg = null;
                    };
                    storageDlg.TryOpen();
                }
                else
                {
                    (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                    storageDlg?.TryClose();
                    storageDlg?.Dispose();
                    storageDlg = null;
                }
            }
            if (packetid == 1001)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                storageDlg?.TryClose();
                storageDlg?.Dispose();
                storageDlg = null;
            }
        }

        public (bool, int quantitySlots) IsContainer(ItemSlot slot )
        {
            var storageBlock = slot.Itemstack.Block;
            var defaultType = storageBlock?.Attributes?["defaultType"]?.ToString();
            var quantitySlots = storageBlock?.Attributes?["quantitySlots"]?[defaultType]?.AsInt();

            if (quantitySlots == 0 || quantitySlots == null) return (false, 0);

            return (true, (int)quantitySlots);
        }

        public bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            fromPlayer = byPlayer;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if(!slot.Empty && inventory.containerBlockSlotsActive < MAX_CONTAINER_BLOC_SLOTS)
            {
                //попытка поставить блок с инвентарем на стеллаж
                //на данный момент проверяем атрибуты по примеру ванильных сундуков

                int slotsCount = 0;
                var storageBlock = slot.Itemstack.Block;

                var isContainerResult = IsContainer(slot);
                var isContainer = isContainerResult.Item1;
                var quantitySlots = isContainerResult.quantitySlots;
                if (isContainer)
                {
                    slotsCount = (int)quantitySlots;
                    AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;

                    if (TryPut(slot, blockSel, storageBlock))
                    {
                        if (slotsCount > 0)
                        {
                            //записываем сколько и какие конкретно дал слоты данный контейнер, нужно для логики дать/забрать контейнер со стеллажа
                            int lastId = inventory[inventory.Count - 1].SlotId;
                            int[] quantitySlotsId = Enumerable.Range(lastId + 1, quantitySlots).ToArray();

                            lock (inventory.LockContainerSlots)
                            {
                                inventory.ContainerSlots.Add(inventory.containerBlockSlotsActive, quantitySlotsId);
                            }
                            //увеличиваем слоты стеллажа
                            inventory.AddSlots(slotsCount);
                            inventory.dynamicSlots += slotsCount;
                            inventory.containerBlockSlotsActive++;
                        }

                        Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                        MarkDirty();
                        return true;
                    }
                    return false;
                }   
            }

            if (Api.Side != EnumAppSide.Client)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos.X, Pos.Y, Pos.Z, 1000, data);
                byPlayer.InventoryManager.OpenInventory(inventory);
            }
            MarkDirty();
            return true;
        }

        bool TryPut(ItemSlot slot, BlockSelection blockSel, Block storageContainer)
        {
            int blockIndex = blockSel.SelectionBoxIndex + inventory.containerBlockSlotsActive;
            if (inventory[blockIndex].Empty)
            {
                int num = slot.TryPutInto(Api.World, inventory[blockIndex]);
                MarkDirty();

                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }
            return false;
        }

        public bool Open()
        {
            if (Api.World.Side == EnumAppSide.Client)
            {
                ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1101);
            }
            return true;
        }
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid <= 1000)
            {
                inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
            }
            if (packetid == 1101)
            {
                ICoreServerAPI obj = (ICoreServerAPI)Api;
                if (isOpened)
                {
                    //Добавить звук закрытие 
                }
                else
                {
                    // открыли интерфейс, добавить звук открытия сундука

                }
                isOpened = !isOpened;
                obj.Network.BroadcastBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1101, BitConverter.GetBytes(isOpened));
            }
            if (packetid == 1001 && fromPlayer.InventoryManager != null)
            {
                fromPlayer.InventoryManager.CloseInventory(Inventory);
            }
        }
       

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isOpened", isOpened);
            tree.SetInt("dynamicSlots", inventory.dynamicSlots);
            tree.SetInt("containerBlockSlotsActive", inventory.containerBlockSlotsActive);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            isOpened = tree.GetBool("isOpened");
            inventory.dynamicSlots = tree.GetInt("dynamicSlots");
            inventory.containerBlockSlotsActive = tree.GetInt("containerBlockSlotsActive");
            RedrawAfterReceivingTreeAttributes(worldAccessForResolve);
        }

        public override void updateMeshes()
        {
            base.updateMeshes();
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[3][];
            float scale = 0.9f;
            float x = 0;
            float z = 0;
            float y = 0;
            for (int index = 0; index < 3; index++)
            {
                if(index == 0)
                {
                    x = 1.02f;
                    z = 0.05f;
                    y = 0f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(block.Shape.rotateY)// Сначала перемещаем предмет в центр блока
                       .Translate(x - 1f, y, z) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(scale, scale, scale)
                       .Values;

                }
                if (index == 1)
                {
                    x = 1.02f;
                    z = 0.05f; 
                    y = 1f; 
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(block.Shape.rotateY)
                       .Translate(x - 1f, y, z) 
                       .Translate(-0.5f, 0f, -0.5f) 
                       .Scale(scale, scale, scale)
                       .Values;
                }
                if (index == 2)
                {
                    x = 1.02f;
                    z = 0.05f;
                    y = 2f; 
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(block.Shape.rotateY)
                       .Translate(x - 1f, y, z) 
                       .Translate(-0.5f, 0f, -0.5f) 
                       .Scale(scale, scale, scale)
                       .Values;
                }
            }
            return tfMatrices;
        }

    }
}
