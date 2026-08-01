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
    public class BEDrawerTopped : BlockEntityDisplay
    {
        InventoryDrawer inv;
        public override InventoryDrawer Inventory => inv;
        public override string InventoryClassName => "drawertopped";

        public override string AttributeTransformCode => "oncrateclosedTransform";
        public override int DisplayedItems => 16;
        Block block;
        GuiDialogDrawerTopped storageDlg;

        static int slotCount = 16;
        IPlayer byPlayer;

        private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;
        private bool _isOpen = false;

        public BEDrawerTopped()
        {
            inv = new InventoryDrawer("drawertopped-0", slotCount, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);


            inv.SlotModified += OnInventorySlotModified;

            if (api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                if (capi == null) return;

                AssetLocation shapeLoc = new AssetLocation("moreinventorys:shapes/drawertopped.json");
                Shape shape = Shape.TryGet(capi, shapeLoc);
                if (shape == null) return;

                animUtil?.InitializeAnimator("drawertopped", shape, null, new Vec3f(0, block.Shape.rotateY, 0));
            }
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

            if (inv != null)
            {
                inv.SlotModified -= OnInventorySlotModified;
            }

            base.OnBlockRemoved();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (inv != null)
            {
                inv.SlotModified -= OnInventorySlotModified;
            }
            storageDlg = null;
        }





        private void OnInventorySlotModified(int slotid)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                MarkDirty(true);
                updateMeshes();
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
            }
        }


        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                if (_isOpen)
                {
                    _isOpen = false;
                    ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1103);
                }
                else
                {
                    _isOpen = true;
                    ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1102);
                }
                MarkDirty(true);
                this.byPlayer = byPlayer;
                OpenGui(byPlayer);
            }
            return true;
        }

        public void OpenAnimation()
        {
            if (animUtil == null) return;
            MarkDirty(true);
            Api.World.PlaySoundAt(new AssetLocation("sounds/block/chestopen"), Pos.X, Pos.Y, Pos.Z);

            animUtil.StartAnimation(new AnimationMetaData()
            {
                Animation = "open",
                Code = "open",
                AnimationSpeed = 3.0f,
                EaseOutSpeed = 12,
                EaseInSpeed = 30
            });
        }

        public void CloseAnimation()
        {
            if (animUtil == null) return;
            MarkDirty(true);
            Api.World.PlaySoundAt(new AssetLocation("sounds/block/chestclose"), Pos.X, Pos.Y, Pos.Z);
            animUtil.StopAnimation("open");
            animUtil.StartAnimation(new AnimationMetaData()
            {
                Animation = "close",
                Code = "close",
                AnimationSpeed = 3.0f,
                EaseOutSpeed = 12,
                EaseInSpeed = 30
            });
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
                    inv.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, new Vec3i(Pos.X, Pos.Y, Pos.Z).AsBlockPos, 1000, data);
                byPlayer.InventoryManager.OpenInventory(inv);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            // Анимации от сервера
            if (packetid == 1102 && Api.Side == EnumAppSide.Client)
            {
                OpenAnimation();
                return;
            }
            if (packetid == 1103 && Api.Side == EnumAppSide.Client)
            {
                CloseAnimation();
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
                    storageDlg = new GuiDialogDrawerTopped(Lang.Get("moreinventorys:block-drawertopped-north"), (InventoryGeneric)Inventory, Pos, Api as ICoreClientAPI);

                    storageDlg.OnClosed += delegate
                    {
                        Open();
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

            // Сервер получил пакет о закрытии GUI от клиента
            if (packetid == 1001 && Api.Side == EnumAppSide.Server)
            {
                _isOpen = false;
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1103);
                MarkDirty(true);
                // Закрываем инвентарь у игрока
                if (storageDlg != null)
                {
                    (Api.World as IClientWorldAccessor)?.Player.InventoryManager.CloseInventory(Inventory);
                    storageDlg?.TryClose();
                    storageDlg?.Dispose();
                    storageDlg = null;
                }
                return;
            }
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
                inv.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
            }
            // Клиент отправил пакет о закрытии GUI → передаём на сервер
            if (packetid == 1001 && Api.Side == EnumAppSide.Server)
            {
                _isOpen = false;
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 1103);
                MarkDirty(true);
                if (storageDlg != null)
                {
                    (Api.World as IClientWorldAccessor)?.Player.InventoryManager.CloseInventory(Inventory);
                    storageDlg?.TryClose();
                    storageDlg?.Dispose();
                    storageDlg = null;
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            _isOpen = tree.GetBool("isOpen", false);
            RedrawAfterReceivingTreeAttributes(worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isOpen", _isOpen);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];
            float scale = 0.5f;
            float scaleShelf = 0.001f;
            float x = 0.8f;
            float z = 0;
            float y = 0;

            int orientationRotate = 0;
            string code = "";
            for (int index = 0; index < slotCount; index++)
            {
                //ВЕРХНЯЯ ПОЛКА
                if(index <= 1)
                {
                    x += 0.3f;
                    z = 0.05f;
                    y = 1f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                    if (index == 1) x = 0.8f;
                }
                else if (index <= 3)
                {
                    x += 0.3f;
                    z = 0.4f;
                    y = 1f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;

                    if (index == 3) x = 0.8f;
                }
                else if (index <= 7) //ЯЩИК
                {
                    x += 0.3f;
                    z = 0.1f;
                    y = 0.55f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scaleShelf, scaleShelf, scaleShelf)
                       .Values;
                    if (index == 7) x = 0.8f;
                }
                else if (index <= 11) //ЯЩИК
                {
                    x += 0.3f;
                    z = 0.4f;
                    y = 0.55f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scaleShelf, scaleShelf, scaleShelf)
                       .Values;
                    if (index == 11) x = 0.8f;
                }
                else if (index <= 13) //НИЖНЯЯ ПОЛКА
                {
                    x += 0.3f;
                    z = 0.05f;
                    y = 0.12f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
                       .Translate(x - 1f, y, z)
                       .Translate(-0.5f, 0f, -0.5f)
                       .Scale(scale, scale, scale)
                       .Values;
                    if (index == 13) x = 0.8f;
                }
                else if (index <= 15) //НИЖНЯЯ ПОЛКА
                {
                    x += 0.3f;
                    z = 0.4f;
                    y = 0.12f;

                    tfMatrices[index] = new Matrixf()
                       .Translate(0.5f, 0f, 0.5f)
                       .RotateYDeg(Block.Shape.rotateY)
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