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
    public class BERackVertical1x2 : BlockEntityDisplay
    {
        private const int PACKET_SYNC_STATE = 2000;
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();

        private Dictionary<int, string> storageContainers { get; set; }
        private InventoryFixed inventory;
        private Block block;
        private GuiDialogFixed storageDlg;

        public const int MAX_CONTAINER_BLOC_SLOTS = 2;
        public static IPlayer fromPlayer;

        private string container1;
        private string container2;
        private bool isFirstLoad = true;
        public bool isOpened;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "rackvertical1x2onedynamic";

        public BERackVertical1x2()
        {
            inventory = new InventoryFixed("rackverticalone1x2-0", MAX_CONTAINER_BLOC_SLOTS, null);
            storageContainers = new Dictionary<int, string>();
        }

        public override void Initialize(ICoreAPI api)
        {
            inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            inventory.SlotModified += OnInventorySlotModified;
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);

            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                if (capi == null) return;

                // ✅ Загружаем shape как в ящике
                AssetLocation shapeLoc = new AssetLocation("moreinventorys:shapes/rackvertical1x2.json");
                Shape shape = Shape.TryGet(capi, shapeLoc);
                if (shape == null) return;
            }

            if (api.Side == EnumAppSide.Server && !(api is ICoreClientAPI))
            {
                api.Event.RegisterCallback(dt => {
                    BroadcastStateToNearbyPlayers();
                }, 100);
            }
        }

        // ==================== СИНХРОНИЗАЦИЯ ====================
        private void BroadcastStateToNearbyPlayers()
        {
            if (Api.Side != EnumAppSide.Server || Api is ICoreClientAPI) return;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                TreeAttribute tree = new TreeAttribute();
                ToTreeAttributes(tree);
                tree.ToBytes(writer);
                byte[] data = ms.ToArray();

                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, PACKET_SYNC_STATE, data, null);
            }
        }

        private void SendStateToPlayer(IPlayer player)
        {
            if (Api.Side != EnumAppSide.Server || Api is ICoreClientAPI) return;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                TreeAttribute tree = new TreeAttribute();
                ToTreeAttributes(tree);
                tree.ToBytes(writer);
                byte[] data = ms.ToArray();

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)player, Pos, PACKET_SYNC_STATE, data);
            }
        }

        // ==================== ОБРАБОТЧИК ИЗМЕНЕНИЯ СЛОТА ====================
        private void OnInventorySlotModified(int slotid)
        {
            // ✅ ТОЛЬКО НА КЛИЕНТЕ (как в ящике)
            if (Api.Side == EnumAppSide.Client)
            {
                MarkDirty(true);
                updateMeshes();
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
            }
        }

        // ==================== ОТПИСКИ ====================
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (inventory != null)
            {
                inventory.SlotModified -= OnInventorySlotModified;
            }
            storageDlg = null;
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (inventory != null)
            {
                inventory.SlotModified -= OnInventorySlotModified;
            }
            storageDlg = null;
        }

        // ==================== ОБРАБОТКА ПАКЕТОВ ====================
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            // Анимации (как в ящике, если нужны)
            if (packetid == 1102 && Api.Side == EnumAppSide.Client)
            {
                // OpenAnimation();
                return;
            }
            if (packetid == 1103 && Api.Side == EnumAppSide.Client)
            {
                // CloseAnimation();
                return;
            }

            if (packetid == 1101)
            {
                isOpened = BitConverter.ToBoolean(data, 0);
                return;
            }

            if (packetid == 1000)
            {
                using MemoryStream ms = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(ms);
                TreeAttribute tree = new TreeAttribute();
                tree.FromBytes(reader);
                Inventory.FromTreeAttributes(tree);
                Inventory.ResolveBlocksOrItems();

                if (storageDlg == null)
                {
                    Open();
                    Api.World.PlaySoundAt(new AssetLocation("moreinventorys:sounds/barrelopen.ogg"), Pos.X, Pos.Y, Pos.Z);
                    storageDlg = new GuiDialogFixed(inventory.ActiveSlots, inventory.ActiveSlotsCount, Lang.Get("moreinventorys:block-rackverticalone1x2"), (InventoryFixed)Inventory, Pos, Api as ICoreClientAPI);

                    storageDlg.OnClosed += delegate
                    {
                        Open();
                        Api.World.PlaySoundAt(new AssetLocation("moreinventorys:sounds/barrelclose.ogg"), Pos.X, Pos.Y, Pos.Z);
                        if (Api.Side == EnumAppSide.Client)
                        {
                            capi.Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1001);
                        }
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
                return;
            }

            if (packetid == 1001 && Api.Side == EnumAppSide.Server)
            {
                // Сервер получил закрытие GUI
                if (storageDlg != null)
                {
                    (Api.World as IClientWorldAccessor)?.Player.InventoryManager.CloseInventory(Inventory);
                    storageDlg?.TryClose();
                    storageDlg?.Dispose();
                    storageDlg = null;
                }
                return;
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
                    MarkDirty(true);
                    updateMeshes();
                    Api.World.BlockAccessor.MarkBlockDirty(Pos);
                }
                return;
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
                    SendStateToPlayer(fromPlayer);
                    BroadcastStateToNearbyPlayers();
                }
            }
        }

        // ==================== ЛОГИКА ВЗАИМОДЕЙСТВИЯ ====================
        public (bool, int quantitySlots) IsValidContainer(ItemSlot slot)
        {
            var storageBlock = slot.Itemstack.Block;
            if (storageBlock != null)
            {
                if (storageBlock.Code.ToString().Contains("trunk")) return (false, 0);
            }

            string cod = GetValueBeforeDash(slot.Itemstack.Block.Code.Path);
            int? quantitySlots = 0;
            if (!ModConfigFile.Current.VanilaStorageContainersCode.Contains(cod) && !ModConfigFile.Current.ModedStorageContainersCode.ContainsKey(cod))
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

        private string GetValueBeforeDash(string input)
        {
            int indexOfDash = input.IndexOf('-');
            return indexOfDash >= 0 ? input.Substring(0, indexOfDash) : input;
        }

        public bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            fromPlayer = byPlayer;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            // ✅ Проверяем, есть ли контейнер в слоте
            if (!inventory[blockSel.SelectionBoxIndex].Empty)
            {
                // Слот занят — открываем GUI
                OpenGui(byPlayer);
                return true;
            }

            // ✅ Если в руке контейнер и есть место
            if (!slot.Empty && inventory.containerBlockSlotsActive < MAX_CONTAINER_BLOC_SLOTS)
            {
                var storageBlock = slot.Itemstack.Block;
                if (storageBlock == null) return false;

                var (isContainer, quantitySlots) = IsValidContainer(slot);
                if (!isContainer)
                {
                    OpenGui(byPlayer);
                    return true;
                }

                // ✅ Ставим контейнер на стеллаж
                if (TryPut(slot, blockSel, storageBlock))
                {
                    if (quantitySlots > 0)
                    {
                        inventory.AddContainerSlots(blockSel.SelectionBoxIndex, quantitySlots);

                        if (storageBlock.Code.Path != "")
                        {
                            storageContainers[blockSel.SelectionBoxIndex] = storageBlock.Code.Path + DateTime.Now.ToString();
                        }

                        switch (blockSel.SelectionBoxIndex)
                        {
                            case 0: container1 = storageBlock.Code.Path; break;
                            case 1: container2 = storageBlock.Code.Path; break;
                        }

                        inventory.containerBlockSlotsActive++;
                    }

                    MoreInventorysMod.PlaySoundBlockAt(Api, slot, byPlayer);

                    // ✅ Отправляем синхронизацию
                    if (Api.Side == EnumAppSide.Server)
                    {
                        SendStateToPlayer(byPlayer);
                        BroadcastStateToNearbyPlayers();
                    }

                    return true;
                }
                return false;
            }

            // ✅ Если ничего не подошло — открываем GUI
            OpenGui(byPlayer);
            return true;
        }

        private bool TryPut(ItemSlot slot, BlockSelection blockSel, Block storageContainer)
        {
            int blockIndex = blockSel.SelectionBoxIndex;
            if (inventory[blockIndex].Empty)
            {
                inventory.IsTryPut = true;
                int num = slot.TryPutInto(Api.World, inventory[blockIndex]);
                inventory.IsTryPut = false;

                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                return num > 0;
            }
            return false;
        }

        private void OpenGui(IPlayer byPlayer)
        {
            if (Api.Side == EnumAppSide.Client) return;

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

        public bool Open()
        {
            if (Api.World.Side == EnumAppSide.Client)
            {
                ((ICoreClientAPI)Api).Network.SendBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1101);
            }
            return true;
        }

        // ==================== СОХРАНЕНИЕ/ЗАГРУЗКА ====================
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isOpened", isOpened);
            tree.SetInt("containerBlockSlotsActive", inventory.containerBlockSlotsActive);

            tree.SetString("container1", container1);
            tree.SetString("container2", container2);

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
            inventory.containerBlockSlotsActive = tree.GetInt("containerBlockSlotsActive", 0);

            container1 = tree.GetString("container1");
            container2 = tree.GetString("container2");

            if (isFirstLoad)
            {
                isFirstLoad = false;
                RebuildStorageContainers();
            }

            DummyPositions = new List<BlockPos>();
            int count = tree.GetInt("dummyCount");
            for (int i = 0; i < count; i++)
            {
                DummyPositions.Add(new BlockPos(tree.GetInt("dx" + i), tree.GetInt("dy" + i), tree.GetInt("dz" + i)));
            }

            RedrawAfterReceivingTreeAttributes(worldAccessForResolve);
        }

        private void RebuildStorageContainers()
        {
            storageContainers.Clear();
            if (!string.IsNullOrEmpty(container1)) storageContainers[0] = container1;
            if (!string.IsNullOrEmpty(container2)) storageContainers[1] = container2;
        }

        public override void updateMeshes()
        {
            base.updateMeshes();
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
        }

        // ==================== РЕНДЕРИНГ ====================
        private (int, string) GetOrientationRateForMartices(int containerIndex)
        {
            int orientationRotate = 0;

            if (storageContainers.Count == 0)
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (!storageContainers.ContainsKey(containerIndex))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            var container = storageContainers[containerIndex];
            if (string.IsNullOrEmpty(container))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (container.Contains("chest"))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 0;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "chest");
            }
            else if (container.Contains("micrateclosed"))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "micrateclosed");
            }
            else if (container.Contains("mibasketclosed"))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "mibasketclosed");
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
            // ✅ КАК В ЯЩИКЕ — возвращаем пустой массив
            return new float[0][];
        }
    }
}