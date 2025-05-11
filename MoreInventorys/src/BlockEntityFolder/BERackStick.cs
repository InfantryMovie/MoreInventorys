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
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();

        //словарь с контейнерами на стеллажах, для корректного отображения на полках
        Dictionary<int, string> storageContainers;

        //ссылки на слоты контейнеров на стеллажах для сохранения в дереве
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

        //число слотов для инвентарей которые будут установлены на стеллажи
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
                    Api.World.PlaySoundAt(new AssetLocation("moreinventorys:sounds/barrelopen.ogg"), Pos.X, Pos.Y, Pos.Z);
                    storageDlg = new GuiDialogDynamic(inventory.dynamicSlots, Lang.Get("moreinventorys:rackhorizontal-title"), (InventoryDynamic)Inventory, Pos, Api as ICoreClientAPI);
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
        }


        public (bool, int quantitySlots) IsValidContainer(ItemSlot slot)
        {
            string cod = GetValueBeforeDash(slot.Itemstack.Block.Code.Path);
            int? quantitySlots = 0;
            //if (!ModConfigFile.Current.StorageContainersCode.Contains(cod)) return (false, 0);

            if (cod == "stationarybasket")
            {
                //BlockGenericTypedContainer
                string type = slot.Itemstack.Attributes.GetString("type");
                if (type != null)
                {
                    int? num = slot.Itemstack.ItemAttributes?["quantitySlots"]?[type]?.AsInt();
                    if (num != null) quantitySlots = (int)num;
                }
            }


            if (quantitySlots == 0 || quantitySlots == null) return (false, 0);

            return (true, (int)quantitySlots);
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

        public bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!slot.Empty && inventory.containerBlockSlotsActive < MAX_CONTAINER_BLOC_SLOTS)
            {
                //попытка поставить блок с инвентарем на стеллаж

                int slotsCount = 0;
                var storageBlock = slot.Itemstack.Block;
                if (storageBlock.Code == null) return false;

                var isContainerResult = IsValidContainer(slot);
                var isContainer = isContainerResult.Item1;
                var quantitySlots = isContainerResult.quantitySlots;

                if (!isContainer) return false;

                slotsCount = (int)quantitySlots;
                AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
                if (slotsCount <= 0) return false;

                if (TryPut(slot, blockSel, storageBlock))
                {
                    //записываем сколько и какие конкретно дал слоты данный контейнер, нужно для логики дать/забрать контейнер со стеллажа (временно не работает!)
                    int lastId = inventory[inventory.Count - 1].SlotId;
                    int[] quantitySlotsId = Enumerable.Range(lastId + 1, quantitySlots).ToArray();

                    lock (inventory.LockContainerSlots)
                    {
                        inventory.ContainerSlots.Add(inventory.containerBlockSlotsActive, quantitySlotsId);
                    }

                    if (storageBlock.Code.Path != "" && storageContainers.Count != MAX_CONTAINER_BLOC_SLOTS)
                    {
                        storageContainers.Add(inventory.containerBlockSlotsActive, storageBlock.Code.Path + DateTime.Now.ToString());
                    }

                    switch (inventory.containerBlockSlotsActive)
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

                    //увеличиваем слоты стеллажа
                    inventory.AddSlots(slotsCount);
                    inventory.dynamicSlots += slotsCount;
                    inventory.containerBlockSlotsActive++;

                    Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                    MarkDirty();
                    return true;
                }
                return false;
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
                }
                else
                {


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
                bool num = InitializeStorageContainers();
            }

            DummyPositions = new List<BlockPos>();
            int count = tree.GetInt("dummyCount");
            for (int i = 0; i < count; i++)
            {
                DummyPositions.Add(new BlockPos(tree.GetInt("dx" + i), tree.GetInt("dy" + i), tree.GetInt("dz" + i)));
            }


            RedrawAfterReceivingTreeAttributes(worldAccessForResolve);

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
                //двойной сундук
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "trunk");
            }
            else if (container.Contains("chest"))
            {
                //сундук
                if (Block.Variant["horizontalorientation"] == "east") orientationRotate = 0;
                if (Block.Variant["horizontalorientation"] == "south") orientationRotate = 270;
                if (Block.Variant["horizontalorientation"] == "west") orientationRotate = 180;
                if (Block.Variant["horizontalorientation"] == "north") orientationRotate = 90;
                return (orientationRotate, "chest");
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
            float[][] tfMatrices = new float[4][];
            float scale = 0.9f;
            float x = 0;
            float z = 0;
            float y = 0;

            int orientationRotate = 0;
            string code = "";
            for (int index = 0; index < 4; index++)
            {
                orientationRotate = GetOrientationRateForMartices(index).Item1;
                code = GetOrientationRateForMartices(index).Item2;

                if (index == 0)
                {

                    x = 1.02f;
                    z = 0.05f;
                    y = 0f;
                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(orientationRotate)// Сначала перемещаем предмет в центр блока
                       .Translate(x - 1f, y, z) // Двигаем предмет на нужные координаты (x, y, z)
                       .Translate(-0.5f, 0f, -0.5f) // Возвращаем в локальную систему координат блока
                       .Scale(scale, scale, scale)
                       .Values;

                }
                if (index == 1)
                {
                    x = 2.04f;
                    z = 0.05f;
                    y = 0f;
                    if (code == "chest")
                    {
                        z += 1;
                        x = 1;
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
                    if (code == "chest")
                    {
                        z += 1;
                        x = 1;
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
