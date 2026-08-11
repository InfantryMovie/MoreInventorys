using MoreInventorys.src.BlockEntityFolder.Interface;
using MoreInventorys.src.GuiFolder;
using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
    internal abstract class BERackBase : BlockEntityDisplay
    {
        protected const int PACKET_SYNC_STATE = 2000;
        protected InventoryDynamic inventory;
        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName { get; }
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();
        public bool IsOpened;
        protected bool isFirstLoad = true;
        protected Block block;
        protected GuiDialogDynamic storageDlg;
        // Словарь для хранения информации о размещенных контейнерах
        protected Dictionary<int, string> StorageContainers { get; set; }
        //ссылки на слоты контейнеров на стеллажах для сохранения в дереве
        protected string container1;
        protected string container2;


        protected int _containerCounter = 0;

        // Абстрактные свойства для определения размеров сетки
        public int Columns { get; }  // Количество колонок (по горизонтали)
        public int Rows { get; }     // Количество строк (по вертикали)
        public abstract string GuiTitle { get; }
        public int MaxContainerSlots { get; }
        public override int DisplayedItems { get; }
        public bool isOpened;
        public int MaxDoubleChests => Columns < 2
                ? 0
                : (Columns / 2) * Rows;

        protected int doubleChestIndex1;
        protected int doubleChestIndex2;
        protected bool isDoubleChestEnable { get; set; }



        public BERackBase(string inventoryClassName, int colums, int rows, bool isDoubleChestEnable, int displayedItems)
        {
            Columns = colums;
            Rows = rows;
            MaxContainerSlots = Columns * Rows;
            DisplayedItems = MaxContainerSlots;
            InventoryClassName = inventoryClassName;
            inventory = new InventoryDynamic($"{InventoryClassName}-0", MaxContainerSlots, null);
            StorageContainers = new Dictionary<int, string>();
            this.isDoubleChestEnable = isDoubleChestEnable;
            if(isDoubleChestEnable)
            {
                doubleChestIndex1 = -1;
                doubleChestIndex2 = -1;
            }


        }

        public override void Initialize(ICoreAPI api)
        {
            inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            inventory.SlotModified += OnSlotModified;
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);

            // При инициализации на сервере отправляем состояние всем
            if (api.Side == EnumAppSide.Server && !(api is ICoreClientAPI))
            {
                // Небольшая задержка для полной инициализации
                api.Event.RegisterCallback(dt => {
                    BroadcastStateToNearbyPlayers();
                }, 100);
            }

        }

        protected virtual bool SetContainerCode(int index, string code)
        {
            switch (index)
            {
                case 0:
                    container1 = code;
                    break;

                case 1:
                    container2 = code;
                    break;

                default:
                    break;
            }

            return true;
        }

        public virtual bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!slot.Empty && inventory.containerBlockSlotsActive < MaxContainerSlots)
            {

                // Проверяем, не занят ли слот (учитывая двойные сундуки)
                if (IsSlotOccupied(blockSel.SelectionBoxIndex))
                {
                    // Слот занят - открываем GUI
                    OpenGui(byPlayer);
                    return true;
                }

                int slotsCount = 0;
                var storageBlock = slot.Itemstack.Block;
                if (storageBlock == null)
                {
                    OpenGui(byPlayer);
                    return true;
                }

                if (storageBlock.Code == null)
                {
                    OpenGui(byPlayer);
                    return true;
                }

                if ((storageBlock.Code.GetName().Contains("trunk") && !isDoubleChestEnable) || (storageBlock.Code.GetName().Contains("micratecloseddouble") && !isDoubleChestEnable))
                {
                    OpenGui(byPlayer);
                    return true;
                }

                var isContainerResult = IsValidContainer(slot);
                var isContainer = isContainerResult.Item1;
                var quantitySlots = isContainerResult.quantitySlots;

                slotsCount = (int)quantitySlots;
                bool isLegitDoubleChest = true;
                int targetSlotIndex = blockSel.SelectionBoxIndex;
                if ((storageBlock.Code.GetName().Contains("trunk") && isDoubleChestEnable) || (storageBlock.Code.GetName().Contains("micratecloseddouble") && isDoubleChestEnable))
                {
                    int leftSlot = targetSlotIndex % 2 == 0 ? targetSlotIndex : targetSlotIndex - 1;

                    if (leftSlot >= 0 && leftSlot < MaxContainerSlots - 1)
                    {
                        bool leftFree = inventory[leftSlot].Empty && !inventory.DoubleChestIndex.Contains(leftSlot);
                        bool rightFree = inventory[leftSlot + 1].Empty && !inventory.DoubleChestIndex.Contains(leftSlot);

                        if (!leftFree || !rightFree)
                        {
                            isLegitDoubleChest = false;
                        }
                        else
                        {
                            targetSlotIndex = leftSlot;
                        }
                    }
                    else
                    {
                        isLegitDoubleChest = false;
                    }
                }

                if (isContainer && isLegitDoubleChest)
                {
                    string type = slot.Itemstack.Attributes.GetString("type");
                    if (storageBlock.Code.Path != "" && StorageContainers.Count != MaxContainerSlots)
                    {
                        string containerKey = storageBlock.Code.Path;

                        if (!string.IsNullOrEmpty(type))
                        {
                            containerKey += "-" + type;
                        }

                        StorageContainers.Add(targetSlotIndex, containerKey + DateTime.Now.ToString());
                        inventory.ContainerTypes[targetSlotIndex] = storageBlock.Code.Path;
                    }



                    if (TryPut(slot, targetSlotIndex, storageBlock, isLegitDoubleChest))
                    {
                       
                        int lastId = inventory[inventory.Count - 1].SlotId;
                        int[] quantitySlotsId = Enumerable.Range(lastId + 1, quantitySlots).ToArray();

                        lock (inventory.LockContainerSlots)
                        {
                            inventory.ContainerSlots[targetSlotIndex] = quantitySlotsId;
                            _containerCounter++;
                        }
                        var scResult = SetContainerCode(targetSlotIndex, storageBlock.Code.Path);
                       

                        inventory.AddSlots(slotsCount);
                        inventory.dynamicSlots += slotsCount;
                        if ((storageBlock.Code.GetName().Contains("trunk") && isDoubleChestEnable) || (storageBlock.Code.GetName().Contains("micratecloseddouble") && isDoubleChestEnable))
                        {
                            inventory.containerBlockSlotsActive++;
                            inventory.DoubleChestIndex.Add(targetSlotIndex);

                            var result = AddDoubleChestIndex(targetSlotIndex);
                        }
                        inventory.containerBlockSlotsActive++;

                        MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);

                        UpdateAllMeshes();

                        UpdateShape();

                        if (Api.Side == EnumAppSide.Server)
                        {
                            SendStateToPlayer(byPlayer);
                        }
                        return true;
                    }
                }
                else
                {
                    OpenGui(byPlayer);
                    return true;
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
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, new Vec3i(Pos.X, Pos.Y, Pos.Z).AsBlockPos, 1000, data);
                byPlayer.InventoryManager.OpenInventory(inventory);
            }


            MarkDirty();
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

        bool TryPut(ItemSlot slot, int blockSelIndex, Block storageContainer, bool isLegitDoubleChest)
        {
            if (!isLegitDoubleChest) return false;

            if (IsSlotOccupied(blockSelIndex)) return false;

            if (inventory[blockSelIndex].Empty)
            {
                inventory.IsTryPut = true;
                int num = slot.TryPutInto(Api.World, inventory[blockSelIndex]);
                inventory.IsTryPut = false;
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }
            return false;
        }
        public virtual (bool, int quantitySlots) IsValidContainer(ItemSlot slot)
        {
            string cod = GetValueBeforeDash(slot.Itemstack.Block.Code.Path);
            int? quantitySlots = 0;
            if (!ModConfigFile.Current.VanilaStorageContainersCode.Contains(cod) &&
                !ModConfigFile.Current.ModedStorageContainersCode.ContainsKey(cod))
                return (false, 0);


            if (ModConfigFile.Current.VanilaStorageContainersCode.Contains(cod))
            {
                string type = slot.Itemstack.Attributes.GetString("type");
                if (type != null)
                {
                    int? num = slot.Itemstack.ItemAttributes?["quantitySlots"]?[type]?.AsInt();
                    if (num != null) quantitySlots = (int)num;
                }

            }

            if (ModConfigFile.Current.ModedStorageContainersCode.ContainsKey(cod))
            {
                quantitySlots = ModConfigFile.Current.ModedStorageContainersCode[cod];
            }

            if (quantitySlots == 0 || quantitySlots == null) return (false, 0);

            return (true, (int)quantitySlots);
        }

        protected virtual bool IsSlotOccupied(int slotIndex)
        {
            // Проверяем, не занят ли слот обычным контейнером
            if (!inventory[slotIndex].Empty) return true;

            // Проверяем, не является ли слот частью двойного сундука
            if (slotIndex % 2 == 1) // 1, 3, 5
            {
                int leftSlot = slotIndex - 1;
                if (inventory.DoubleChestIndex.Contains(leftSlot)) return true;
            }

            if (slotIndex % 2 == 0) // 0, 2, 4
            {
                if (inventory.DoubleChestIndex.Contains(slotIndex)) return true;
            }

            return false;
        }

        protected virtual bool AddDoubleChestIndex(int index)
        {
            if (doubleChestIndex1 == -1)
            {
                doubleChestIndex1 = index;
                return true;
            }

            if (doubleChestIndex2 == -1)
            {
                doubleChestIndex2 = index;
                return true;
            }
            return true;
        }
        private void OpenGui(IPlayer byPlayer)
        {
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
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, new Vec3i(Pos.X, Pos.Y, Pos.Z).AsBlockPos, 1000, data);
                byPlayer.InventoryManager.OpenInventory(inventory);
            }
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
                isOpened = !isOpened;
                obj.Network.BroadcastBlockEntityPacket(new Vec3i(Pos.X, Pos.Y, Pos.Z).AsBlockPos, 1101, BitConverter.GetBytes(isOpened));
            }
            if (packetid == 1001 && fromPlayer.InventoryManager != null)
            {
                fromPlayer.InventoryManager.CloseInventory(Inventory);
                if (Api.Side == EnumAppSide.Server)
                {
                    // Отправляем состояние этому игроку
                    SendStateToPlayer(fromPlayer);
                    // И всем остальным тоже
                    BroadcastStateToNearbyPlayers();
                }
            }
        }

        private void OnSlotModified(int slotid)
        {
            if (Api.World.Side == EnumAppSide.Client) return;

            updateMesh(slotid);
            MarkDirty(true);

            UpdateShape();
        }

        public void UpdateAllMeshes()
        {
            for (int i = 0; i < MaxContainerSlots; i++)
            {
                updateMesh(i);
            }
            MarkDirty(true);
        }

        public void UpdateShape()
        {
            if (Api.Side == EnumAppSide.Server && !(Api is ICoreClientAPI))
            {
                BroadcastStateToNearbyPlayers();
            }
            else if (Api.Side == EnumAppSide.Client)
            {
                updateMeshes();
                MarkDirty(true);
            }
        }

        private void BroadcastStateToNearbyPlayers()
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (Api is ICoreClientAPI) return;


            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                TreeAttribute tree = new TreeAttribute();
                ToTreeAttributes(tree);
                tree.ToBytes(writer);
                byte[] data = ms.ToArray();

                // Отправляем только игрокам, у которых загружен этот чанк
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(
                    Pos,
                    PACKET_SYNC_STATE,
                    data,
                    null
                );
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            // При размещении блока отправляем состояние всем игрокам
            if (Api?.Side == EnumAppSide.Server && !(Api is ICoreClientAPI))
            {
                Api.Event.RegisterCallback(dt => {
                    BroadcastStateToNearbyPlayers();
                }, 100);
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (inventory != null)
            {
                inventory.SlotModified -= OnSlotModified;
            }

            storageDlg = null;
        }

        public override void OnBlockRemoved()
        {
            if (storageDlg != null)
            {
                var dlg = storageDlg;
                storageDlg = null;

                dlg.TryClose();
                dlg.Dispose();
            }

            if (inventory != null)
            {
                inventory.SlotModified -= OnSlotModified;
            }

            storageDlg = null;

            base.OnBlockRemoved();
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
                    Api.World.PlaySoundAt(new AssetLocation("moreinventorys:sounds/barrelopen.ogg"), Pos.X, Pos.Y, Pos.Z);
                    storageDlg = new GuiDialogDynamic(inventory.dynamicSlots, GuiTitle, (InventoryDynamic)Inventory, Pos, Api as ICoreClientAPI);
                    storageDlg.OnClosed += delegate
                    {
                        Open();
                        Api.World.PlaySoundAt(new AssetLocation("moreinventorys:sounds/barrelclose.ogg"), Pos.X, Pos.Y, Pos.Z);
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
            if (packetid == PACKET_SYNC_STATE)
            {
                using MemoryStream ms = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(ms);
                TreeAttribute tree = new TreeAttribute();
                tree.FromBytes(reader);

                Inventory.FromTreeAttributes(tree);
                Inventory.ResolveBlocksOrItems();

                FromTreeAttributes(tree, Api.World);

                RebuildStorageContainers();

                if (Api.Side == EnumAppSide.Client)
                {
                    // ✅ Обновляем все меши
                    UpdateAllMeshes();
                    MarkDirty(true);
                }
            }
        }

        public bool Open()
        {
            if (Api.World.Side == EnumAppSide.Client)
            {
                ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(new Vec3i(Pos.X, Pos.Y, Pos.Z).AsBlockPos , 1101);
            }
            return true;
        }

        protected virtual void RebuildStorageContainers()
        {
            StorageContainers.Clear();

            for (int i = 0; i < MaxContainerSlots; i++)
            {
                string containerCode = i switch
                {
                    0 => container1,
                    1 => container2,
                    _ => ""
                };

                if (!string.IsNullOrEmpty(containerCode))
                {
                    StorageContainers[i] = containerCode;
                }
            }
        }


        private void SendStateToPlayer(IPlayer player)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (Api is ICoreClientAPI) return;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                TreeAttribute tree = new TreeAttribute();
                ToTreeAttributes(tree);
                tree.ToBytes(writer);
                byte[] data = ms.ToArray();

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)player,
                    Pos,
                    PACKET_SYNC_STATE,
                    data
                );
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBool("isOpened", isOpened);
            tree.SetInt("dynamicSlots", inventory.dynamicSlots);
            tree.SetInt("containerBlockSlotsActive", inventory.containerBlockSlotsActive);

            tree.SetString("container1", container1);
            tree.SetString("container2", container2);


            tree.SetInt("_containerCounter", _containerCounter);

            tree.SetInt("doubleChestIndex1", doubleChestIndex1);
            tree.SetInt("doubleChestIndex2", doubleChestIndex2);

            tree.SetInt("dummyCount", DummyPositions.Count);
            for (int i = 0; i < DummyPositions.Count; i++)
            {
                tree.SetInt("dx" + i, DummyPositions[i].X);
                tree.SetInt("dy" + i, DummyPositions[i].Y);
                tree.SetInt("dz" + i, DummyPositions[i].Z);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            isOpened = tree.GetBool("isOpened");
            inventory.dynamicSlots = tree.GetInt("dynamicSlots");
            inventory.containerBlockSlotsActive = tree.GetInt("containerBlockSlotsActive");
            container1 = tree.GetString("container1");
            container2 = tree.GetString("container2");


            _containerCounter = tree.GetInt("_containerCounter");

            doubleChestIndex1 = tree.GetInt("doubleChestIndex1");
            doubleChestIndex2 = tree.GetInt("doubleChestIndex2");

            DummyPositions = new List<BlockPos>();
            int count = tree.GetInt("dummyCount");
            for (int i = 0; i < count; i++)
            {
                DummyPositions.Add(new BlockPos(tree.GetInt("dx" + i), tree.GetInt("dy" + i), tree.GetInt("dz" + i)));
            }

            RedrawAfterReceivingTreeAttributes(worldAccessForResolve);
        }


        protected (int, string) GetOrientationRateForMartices(int containerIndex)
        {

            int orientationRotate = 0;

            if (StorageContainers.Count == 0)
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (!StorageContainers.ContainsKey(containerIndex)) return (orientationRotate, "");

            var container = StorageContainers[containerIndex];
            if (string.IsNullOrEmpty(container))
            {
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "");
            }
            else if (container.Contains("chest") || container.Contains("trunk"))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 0;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, container);
            }
            else
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, container);
            }

        }

        protected virtual float[][] GetTransformationMatrices()
        {
            float[][] tfMatrices = new float[MaxContainerSlots][];
            float scale = 0.9f;
            float x = 0; //Лево/Право
            float z = 0; //Глубина
            float y = 0; //вверх/вниз

            int orientationRotate = 0;
            string code = "";
            for (int index = 0; index < MaxContainerSlots; index++)
            {
                var orientationRotateResult = GetOrientationRateForMartices(index);
                orientationRotate = orientationRotateResult.Item1;
                code = orientationRotateResult.Item2;

                if (index == 0)
                {
                    x = 1.02f;
                    z = 0.05f;
                    y = 0.06f;
                    if (code.Contains("trunk")) z += 0.05f;
                    else if (code.Contains("micratecloseddouble"))
                    {
                        z -= 0.01f;
                        x += 0.08f;
                    }
                    else if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
                    {
                        z -= 0.01f;
                        x += 0.05f;
                    }

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                }
                if (index == 1)
                {
                    x = 2.04f;
                    z = 0.05f;
                    y = 0.06f;
                    if (code.Contains("chest"))
                    {
                        z += 1;
                        x = 1;
                    }
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
                    {
                        z -= 0.01f;
                        x -= 0.01f;
                    }

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                }
                if (index == 2)
                {
                    x = 1.02f;
                    z = 0.05f;
                    y = 1f;
                    if (code.Contains("trunk")) z += 0.05f;
                    else if (code.Contains("micratecloseddouble"))
                    {
                        z -= 0.01f;
                        x += 0.08f;
                    }
                    else if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
                    {
                        z -= 0.01f;
                        x += 0.05f;
                    }
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                }
                if (index == 3)
                {
                    x = 2.04f;
                    z = 0.05f;
                    y = 1f;
                    if (code.Contains("chest"))
                    {
                        z += 1;
                        x = 1;
                    }
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
                    {
                        z -= 0.01f;
                        x -= 0.01f;
                    }
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                }
                if (index == 4)
                {
                    x = 1.02f;
                    z = 0.05f;
                    y = 2f;
                    if (code.Contains("trunk")) z += 0.05f;
                    else if (code.Contains("micratecloseddouble"))
                    {
                        z -= 0.01f;
                        x += 0.08f;
                    }
                    else if (code.Contains("micrateclosed") || code.Contains("mibasketclosed") )
                    {
                        z -= 0.01f;
                        x += 0.05f;
                    }
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                }
                if (index == 5)
                {
                    x = 2.04f;
                    z = 0.05f;
                    y = 2f;
                    if (code.Contains("chest"))
                    {
                        z += 1;
                        x = 1;
                    }
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed") )
                    {
                        z -= 0.01f;
                        x -= 0.01f;
                    }
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                }
            }
            return tfMatrices;
        }
        protected override float[][] genTransformationMatrices()
        {
            return GetTransformationMatrices();
        }


    }
}
