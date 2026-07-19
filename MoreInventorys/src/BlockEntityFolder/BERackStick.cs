using MoreInventorys.src.GuiFolder;
using MoreInventorys.src.InventoryFolder;
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
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BERackStick : BlockEntityDisplay
    {
        private const int PACKET_SYNC_STATE = 2000;
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();

        Dictionary<int, string> storageContainers { get; set; }

        string container1;
        string container2;
        string container3;
        string container4;

        bool isFirstLoad = true;

        Block block;
        InventoryDynamic inventory;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "rackstickdynamic";

        GuiDialogDynamic storageDlg;

        public const int MAX_CONTAINER_BLOC_SLOTS = 4;
        public bool isOpened;
        public BERackStick()
        {
            inventory = new InventoryDynamic("rackstick-0", MAX_CONTAINER_BLOC_SLOTS, null);
            storageContainers = new Dictionary<int, string>();
        }

        public override void Initialize(ICoreAPI api)
        {
            inventory.LateInitialize(InventoryClassName + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            inventory.SlotModified += OnSlotModified;
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);

            if (api.Side == EnumAppSide.Server && !(api is ICoreClientAPI))
            {
                api.Event.RegisterCallback(dt => {
                    BroadcastStateToNearbyPlayers();
                }, 100);
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

                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, PACKET_SYNC_STATE, data, null);
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

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)player, Pos, PACKET_SYNC_STATE, data);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

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
            base.OnBlockRemoved();
            if (inventory != null)
            {
                inventory.SlotModified -= OnSlotModified;
            }

            storageDlg = null;
        }

        public void UpdateAllMeshes()
        {
            for (int i = 0; i < MAX_CONTAINER_BLOC_SLOTS; i++)
            {
                updateMesh(i);
            }
            MarkDirty(true);
        }

        private void OnSlotModified(int slotid)
        {
            if (Api.World.Side == EnumAppSide.Client) return;

            for (int i = 0; i < MAX_CONTAINER_BLOC_SLOTS; i++)
            {
                updateMesh(i);
            }
            MarkDirty(true);

            UpdateShape();
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

        bool InitializeStorageContainers()
        {
            for (int i = 0; i < MAX_CONTAINER_BLOC_SLOTS; i++)
            {
                switch (i)
                {
                    case 0:
                        if (container1 != "") storageContainers.Add(0, container1);
                        break;

                    case 1:
                        if (container2 != "") storageContainers.Add(1, container2);
                        break;

                    case 2:
                        if (container3 != "") storageContainers.Add(2, container3);
                        break;

                    case 3:
                        if (container4 != "") storageContainers.Add(3, container4);
                        break;

                    default:
                        break;
                }
            }

            return true;
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
                    storageDlg = new GuiDialogDynamic(inventory.dynamicSlots, Lang.Get("moreinventorys:block-rackstick-north"), (InventoryDynamic)Inventory, Pos, Api as ICoreClientAPI);
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
                    UpdateAllMeshes();
                    MarkDirty(true);
                }
            }
        }

        public (bool, int quantitySlots) IsValidContainer(ItemSlot slot)
        {
            string cod = GetValueBeforeDash(slot.Itemstack.Block.Code.Path);
            int? quantitySlots = 0;

            if (cod.Contains("mibasket"))
            {
                quantitySlots = 8;
            }

            if (quantitySlots == 0 || quantitySlots == null) return (false, 0);

            return (true, (int)quantitySlots);
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

        public bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (inventory[blockSel.SelectionBoxIndex].Empty)
            {
                if (!slot.Empty && inventory.containerBlockSlotsActive < MAX_CONTAINER_BLOC_SLOTS)
                {
                    if (slot.Itemstack.Block == null) return false;

                    int slotsCount = 0;
                    var storageBlock = slot.Itemstack.Block;
                    if (storageBlock.Code == null) return false;

                    var isContainerResult = IsValidContainer(slot);
                    var isContainer = isContainerResult.Item1;
                    var quantitySlots = isContainerResult.quantitySlots;

                    slotsCount = (int)quantitySlots;

                    if (isContainer)
                    {
                        if (storageBlock.Code.Path != "" && storageContainers.Count != MAX_CONTAINER_BLOC_SLOTS)
                        {
                            int slotIndex = inventory.containerBlockSlotsActive;
                            if (!storageContainers.ContainsKey(slotIndex))
                            {
                                storageContainers.Add(slotIndex, storageBlock.Code.Path + DateTime.Now.ToString());
                            }
                            else
                            {
                                storageContainers[slotIndex] = storageBlock.Code.Path + DateTime.Now.ToString();
                            }
                        }
                        if (TryPut(slot, blockSel, storageBlock))
                        {
                            int lastId = inventory[inventory.Count - 1].SlotId;
                            int[] quantitySlotsId = Enumerable.Range(lastId + 1, quantitySlots).ToArray();

                            lock (inventory.LockContainerSlots)
                            {
                                inventory.ContainerSlots.Add(inventory.containerBlockSlotsActive, quantitySlotsId);
                            }

                            switch (blockSel.SelectionBoxIndex)
                            {
                                case 0:
                                    container1 = storageBlock.Code.Path;
                                    break;

                                case 1:
                                    container2 = storageBlock.Code.Path;
                                    break;

                                case 2:
                                    container3 = storageBlock.Code.Path;
                                    break;

                                case 3:
                                    container4 = storageBlock.Code.Path;
                                    break;

                                default:
                                    break;
                            }

                            inventory.AddSlots(slotsCount);
                            inventory.dynamicSlots += slotsCount;
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
                }
            }
            else
            {
                OpenGui(byPlayer);
                return true;
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

        bool TryPut(ItemSlot slot, BlockSelection blockSel, Block storageContainer)
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

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isOpened", isOpened);
            tree.SetInt("dynamicSlots", inventory.dynamicSlots);
            tree.SetInt("containerBlockSlotsActive", inventory.containerBlockSlotsActive);

            tree.SetString("container1", container1);
            tree.SetString("container2", container2);
            tree.SetString("container3", container3);
            tree.SetString("container4", container4);

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
            container3 = tree.GetString("container3");
            container4 = tree.GetString("container4");

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

            for (int i = 0; i < MAX_CONTAINER_BLOC_SLOTS; i++)
            {
                string containerCode = i switch
                {
                    0 => container1,
                    1 => container2,
                    2 => container3,
                    3 => container4,
                    _ => ""
                };

                if (!string.IsNullOrEmpty(containerCode))
                {
                    storageContainers[i] = containerCode;
                }
            }
        }

        public override void updateMeshes()
        {
            base.updateMeshes();
        }

        int GetOrientationRateForMartices(int containerIndex)
        {
            int orientationRotate = 0;

            if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
            if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
            if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;

            return orientationRotate;
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[4][];
            float scale = 0.9f;

            for (int index = 0; index < 4; index++)
            {
                int orientationRotate = GetOrientationRateForMartices(index);

                float x, z, y;

                if (index == 0 || index == 2)
                {
                    x = 1.02f;
                    z = 0.05f;
                }
                else
                {
                    x = 2.02f;
                    z = 0.05f;
                }

                y = (index < 2) ? 0f : 1f;

                tfMatrices[index] = new Matrixf()
                   .Translate(0.5f, 0f, 0.5f)
                   .RotateYDeg(orientationRotate)
                   .Translate(x - 1f, y, z)
                   .Translate(-0.5f, 0f, -0.5f)
                   .Scale(scale, scale, scale)
                   .Values;
            }
            return tfMatrices;
        }
    }
}