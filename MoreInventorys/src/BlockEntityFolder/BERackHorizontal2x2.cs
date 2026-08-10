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
    internal class BERackHorizontal2x2 : BERackBase
    {
        public override string GuiTitle { get; }
        string container3;
        string container4;

        public BERackHorizontal2x2() : base("rackhorizontal2x2-0", 2, 2, true, 4)
        {
            GuiTitle = Lang.Get("moreinventorys:block-rackhorizontal2x2-north");
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
            tree.SetString("container3", container3);
            tree.SetString("container4", container4);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            container3 = tree.GetString("container3");
            container4 = tree.GetString("container4");

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
                    if (code == "trunk") z += 0.05f;
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
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
                    if (code == "trunk") z += 0.05f;
                    if (code.Contains("micrateclosed") || code.Contains("mibasketclosed"))
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
               
            }
            return tfMatrices;
        }
    }
}