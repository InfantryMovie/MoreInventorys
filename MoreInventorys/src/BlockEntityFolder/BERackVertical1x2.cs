using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace MoreInventorys.src.BlockEntityFolder
{
    internal class BERackVertical1x2 : BERackBase
    {
        public override string GuiTitle { get; }

        public BERackVertical1x2() : base("rackverticalone1x2-0", 1, 2, false, 2)
        {
            GuiTitle = Lang.Get("moreinventorys:block-rackverticalone1x2-north");
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

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            if (isFirstLoad)
            {
                isFirstLoad = false;
                RebuildStorageContainers();
            }
        }


        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[MaxContainerSlots][];
            float scale = 0.9f;
            float x = 0;
            float z = 0;
            float y = 0;

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
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
                    {
                        z -= 0.01f;
                        x += 0.03f;
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
                    x = 1.02f;
                    z = 0.05f;
                    y = 1f;
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
                    {
                        z -= 0.01f;
                        x += 0.03f;
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