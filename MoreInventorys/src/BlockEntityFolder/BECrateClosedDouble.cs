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
    public class BECrateClosedDouble : BlockEntityDisplay
    {
        public List<BlockPos> DummyPositions { get; set; } = new List<BlockPos>();
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "micratecloseddouble";

        public override string AttributeTransformCode => "oncrateclosedTransform";
        public override int DisplayedItems => 0;
        Block block;
        GuiDialogDynamic storageDlg;

        static int slotCount = 32;
        IPlayer byPlayer;

        private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;
        private bool _isOpen = false;

        public BECrateClosedDouble()
        {
            inv = new InventoryGeneric(slotCount, "micratecloseddouble-0", null);
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

                AssetLocation shapeLoc = new AssetLocation("moreinventorys:shapes/micratecloseddouble.json");
                Shape shape = Shape.TryGet(capi, shapeLoc);
                if (shape == null) return;

                animUtil?.InitializeAnimator("micratecloseddouble", shape, null, new Vec3f(0, block.Shape.rotateY, 0));
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

        public void OpenCrateAnimation()
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

        public void CloseCrateAnimation()
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
                OpenCrateAnimation();
                return;
            }
            if (packetid == 1103 && Api.Side == EnumAppSide.Client)
            {
                CloseCrateAnimation();
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
                    storageDlg = new GuiDialogDynamic(slotCount, Lang.Get("moreinventorys:block-micratecloseddouble-north"), (InventoryGeneric)Inventory, Pos, Api as ICoreClientAPI);

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
            DummyPositions = new List<BlockPos>();
            int count = tree.GetInt("dummyCount");
            for (int i = 0; i < count; i++)
            {
                DummyPositions.Add(new BlockPos(tree.GetInt("dx" + i), tree.GetInt("dy" + i), tree.GetInt("dz" + i)));
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("isOpen", _isOpen);
            tree.SetInt("dummyCount", DummyPositions.Count);
            for (int i = 0; i < DummyPositions.Count; i++)
            {
                tree.SetInt("dx" + i, DummyPositions[i].X);
                tree.SetInt("dy" + i, DummyPositions[i].Y);
                tree.SetInt("dz" + i, DummyPositions[i].Z);
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
        }

        protected override float[][] genTransformationMatrices()
        {
            return new float[0][];
        }

        


    }
}