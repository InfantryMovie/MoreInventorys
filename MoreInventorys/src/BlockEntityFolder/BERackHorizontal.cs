
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;


namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BERackHorizontal : BERackBase
    {
        public override string GuiTitle { get; }
        int doubleChestIndex3;
        string container3;
        string container4;
        string container5;
        string container6;
        public BERackHorizontal() : base("rackhorizontaldynamic", 2, 3, true, 6)
        {
            GuiTitle = Lang.Get("moreinventorys:block-rackhorizontal-north");
            doubleChestIndex3 = -1;
        }

        public override bool OnBlockInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            return base.OnBlockInteract(byPlayer, blockSel);
        }

        protected override bool SetContainerCode(int index, string code)
        {
            switch (index)
            {
                case 0:
                    container1 = code;
                    break;
                case 1:
                    container2 = code;
                    break;
                case 2:
                    container3 = code;
                    break;
                case 3:
                    container4 = code;
                    break;
                case 4:
                    container5 = code;
                    break;
                case 5:
                    container6 = code;
                    break;

                default:
                    break;
            }

            return true;
        }

        protected override void RebuildStorageContainers()
        {
            StorageContainers.Clear();

            for (int i = 0; i < MaxContainerSlots; i++)
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
                    StorageContainers[i] = containerCode;
                }
            }
        }

        protected override bool AddDoubleChestIndex(int index)
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

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == PACKET_SYNC_STATE)
            {
                RebuildStorageContainers();
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            // Только новые поля для этого стеллажа
            tree.SetString("container3", container3);
            tree.SetString("container4", container4);
            tree.SetString("container5", container5);
            tree.SetString("container6", container6);
            tree.SetInt("doubleChestIndex3", doubleChestIndex3);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            container3 = tree.GetString("container3");
            container4 = tree.GetString("container4");
            container5 = tree.GetString("container5");
            container6 = tree.GetString("container6");
            doubleChestIndex3 = tree.GetInt("doubleChestIndex3");

            if (isFirstLoad)
            {
                isFirstLoad = false;
                RebuildStorageContainers();
            }
        }

        protected override float[][] genTransformationMatrices()
        {
            return GetTransformationMatrices();
        }

    }
}