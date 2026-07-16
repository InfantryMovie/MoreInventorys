using MoreInventorys.src.BlockFolder;
using MoreInventorys.src.GuiFolder;
using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BERackHorizontalWood2x3 : BlockEntityDisplay
    {
        private const int PACKET_SYNC_STATE = 2000;
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();

        Dictionary<int, string> storageContainers { get; set; }

        string container1;
        string container2;
        string container3;
        string container4;
        string container5;
        string container6;

        private int _containerCounter = 0;

        int doubleChestIndex1;
        int doubleChestIndex2;
        int doubleChestIndex3;

        bool isFirstLoad = true;

        Block block;
        InventoryDynamic inventory;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "rackhorizontalwood2x3dynamic";

        GuiDialogDynamic storageDlg;

        public const int MAX_CONTAINER_BLOC_SLOTS = 6;
        public bool isOpened;
        public BERackHorizontalWood2x3()
        {
            inventory = new InventoryDynamic("rackhorizontalwood2x3-0", MAX_CONTAINER_BLOC_SLOTS, null);
            storageContainers = new Dictionary<int, string>();
            doubleChestIndex1 = -1;
            doubleChestIndex2 = -1;
            doubleChestIndex3 = -1;
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
                    storageDlg = new GuiDialogDynamic(inventory.dynamicSlots, Lang.Get("moreinventorys:block-rackhorizontalwood2x3-north"), (InventoryDynamic)Inventory, Pos, Api as ICoreClientAPI);
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

        string GetValueBeforeDash(string input)
        {
            int indexOfDash = input.IndexOf('-');

            if (indexOfDash >= 0)
            {
                return input.Substring(0, indexOfDash);
            }

            return input;
        }

        private bool IsSlotOccupied(int slotIndex)
        {
            if (!inventory[slotIndex].Empty) return true;

            if (slotIndex % 2 == 1)
            {
                int leftSlot = slotIndex - 1;
                if (inventory.DoubleChestIndex.Contains(leftSlot)) return true;
            }

            if (slotIndex % 2 == 0)
            {
                if (inventory.DoubleChestIndex.Contains(slotIndex)) return true;
            }

            return false;
        }

        public bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!slot.Empty && inventory.containerBlockSlotsActive < MAX_CONTAINER_BLOC_SLOTS)
            {
                if (IsSlotOccupied(blockSel.SelectionBoxIndex))
                {
                    OpenGui(byPlayer);
                    return true;
                }

                int slotsCount = 0;
                var storageBlock = slot.Itemstack.Block;
                if (storageBlock == null) return false;
                if (storageBlock.Code == null) return false;

                var isContainerResult = IsValidContainer(slot);
                var isContainer = isContainerResult.Item1;
                var quantitySlots = isContainerResult.quantitySlots;

                slotsCount = (int)quantitySlots;
                bool isLegitDoubleChest = true;
                int targetSlotIndex = blockSel.SelectionBoxIndex;
                if (storageBlock.Code.GetName().Contains("trunk"))
                {
                    int leftSlot = targetSlotIndex % 2 == 0 ? targetSlotIndex : targetSlotIndex - 1;

                    if (leftSlot >= 0 && leftSlot < MAX_CONTAINER_BLOC_SLOTS - 1)
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
                    if (storageBlock.Code.Path != "" && storageContainers.Count != MAX_CONTAINER_BLOC_SLOTS)
                    {
                        string containerKey = storageBlock.Code.Path;

                        if (!string.IsNullOrEmpty(type))
                        {
                            containerKey += "-" + type;
                        }

                        storageContainers.Add(targetSlotIndex, containerKey + DateTime.Now.ToString());
                    }

                    if (TryPut(slot, targetSlotIndex, storageBlock, isLegitDoubleChest))
                    {
                        int lastId = inventory[inventory.Count - 1].SlotId;
                        int[] quantitySlotsId = Enumerable.Range(lastId + 1, quantitySlots).ToArray();

                        lock (inventory.LockContainerSlots)
                        {
                            inventory.ContainerSlots.Add(_containerCounter, quantitySlotsId);
                            _containerCounter++;
                        }

                        switch (targetSlotIndex)
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

                            case 4:
                                container5 = storageBlock.Code.Path;
                                break;

                            case 5:
                                container6 = storageBlock.Code.Path;
                                break;

                            default:
                                break;
                        }

                        inventory.AddSlots(slotsCount);
                        inventory.dynamicSlots += slotsCount;
                        if (storageBlock.Code.GetName().Contains("trunk"))
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

        bool AddDoubleChestIndex(int index)
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

            if (doubleChestIndex3 == -1)
            {
                doubleChestIndex3 = index;
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
            tree.SetString("container5", container5);
            tree.SetString("container6", container6);

            tree.SetInt("_containerCounter", _containerCounter);

            tree.SetInt("doubleChestIndex1", doubleChestIndex1);
            tree.SetInt("doubleChestIndex2", doubleChestIndex2);
            tree.SetInt("doubleChestIndex3", doubleChestIndex3);

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
            container5 = tree.GetString("container5");
            container6 = tree.GetString("container6");

            _containerCounter = tree.GetInt("_containerCounter");

            doubleChestIndex1 = tree.GetInt("doubleChestIndex1");
            doubleChestIndex2 = tree.GetInt("doubleChestIndex2");
            doubleChestIndex3 = tree.GetInt("doubleChestIndex3");

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
                    4 => container5,
                    5 => container6,
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

        (int, string) GetOrientationRateForMartices(int containerIndex)
        {
            int orientationRotate = 0;

            if (storageContainers.Count == 0)
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (!storageContainers.ContainsKey(containerIndex)) return (orientationRotate, "");

            var container = storageContainers[containerIndex];
            if (string.IsNullOrEmpty(container))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 90;
                return (orientationRotate, "");
            }

            if (container.Contains("trunk"))
            {
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "trunk");
            }
            else if (container.Contains("chest"))
            {
                string type = "normal-generic";
                int typeStartIndex = container.IndexOf("chest-") + 6;
                if (typeStartIndex > 5 && typeStartIndex < container.Length)
                {
                    string extractedType = container.Substring(typeStartIndex);
                    int dateIndex = extractedType.IndexOf(' ');
                    if (dateIndex > 0) extractedType = extractedType.Substring(0, dateIndex);
                    if (!string.IsNullOrEmpty(extractedType)) type = extractedType;
                }

                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 0;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "chest-" + type);
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
            else if (container.Contains("storagevessel"))
            {
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 0;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "storagevessel");
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
            float[][] tfMatrices = new float[MAX_CONTAINER_BLOC_SLOTS][];
            float scale = 0.7f;
            float x = 0;
            float z = 0;
            float y = 0;

            int orientationRotate = 0;
            string code = "";
            for (int index = 0; index < MAX_CONTAINER_BLOC_SLOTS; index++)
            {
                var orientationRotateResult = GetOrientationRateForMartices(index);
                orientationRotate = orientationRotateResult.Item1;
                code = orientationRotateResult.Item2;

                if (index == 0)
                {
                    x = 1.02f;
                    z = 0.05f;
                    y = 0.25f;
                    if (code == "trunk")
                    {
                        z += 0.215f;
                        x += 0.17f;
                    }
                    if (code.Contains("micrateclosed"))
                    {
                        z += 0.05f;
                        x += 0.16f;
                    }
                    if (code.Contains("mibasketclosed"))
                    {
                        z += 0.05f;
                        x += 0.16f;
                    }
                    if (code.Contains("chest") || code.Contains("storagevessel"))
                    {
                        z += 0.14f;
                        x += 0.17f;
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
                    y = 0.25f;
                    if (code.Contains("chest") || code.Contains("storagevessel"))
                    {
                        z += 1f;
                        x -= 0.85f;
                    }
                    if (code.Contains("micrateclosed"))
                    {
                        z += 0.05f;
                        x += 0.02f;
                    }
                    if (code.Contains("mibasketclosed"))
                    {
                        z += 0.05f;
                        x += 0.01f;
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
                    if (code == "trunk")
                    {
                        z += 0.215f;
                        x += 0.17f;
                    }
                    if (code.Contains("micrateclosed"))
                    {
                        z += 0.05f;
                        x += 0.16f;
                    }
                    if (code.Contains("mibasketclosed"))
                    {
                        z += 0.05f;
                        x += 0.16f;
                    }
                    if (code.Contains("chest") || code.Contains("storagevessel"))
                    {
                        z += 0.14f;
                        x += 0.17f;
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
                    if (code.Contains("chest") || code.Contains("storagevessel"))
                    {
                        z += 1f;
                        x -= 0.85f;
                    }
                    if (code.Contains("micrateclosed"))
                    {
                        z += 0.05f;
                        x += 0.02f;
                    }
                    if (code.Contains("mibasketclosed"))
                    {
                        z += 0.05f;
                        x += 0.01f;
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
                    if (code == "trunk")
                    {
                        z += 0.215f;
                        x += 0.17f;
                    }
                    if (code.Contains("micrateclosed"))
                    {
                        z += 0.05f;
                        x += 0.16f;
                    }
                    if (code.Contains("mibasketclosed"))
                    {
                        z += 0.05f;
                        x += 0.16f;
                    }
                    if (code.Contains("chest") || code.Contains("storagevessel"))
                    {
                        z += 0.14f;
                        x += 0.17f;
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
                    if (code.Contains("chest") || code.Contains("storagevessel"))
                    {
                        z += 1f;
                        x -= 0.85f;
                    }
                    if (code.Contains("micrateclosed"))
                    {
                        z += 0.05f;
                        x += 0.02f;
                    }
                    if (code.Contains("mibasketclosed"))
                    {
                        z += 0.05f;
                        x += 0.01f;
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
    }
}