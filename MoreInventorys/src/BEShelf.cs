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
using Vintagestory.API.Server;

namespace MoreInventorys.src
{
    // Класс для сущности блока
    public class BEShelf : BlockEntity
    {
        internal InventoryEight inventory;

        public BEShelf()
        {
            inventory = new InventoryEight(null, null);
        }

        public void OnBlockInteract(IPlayer byPlayer)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                // Отправляем пакет на клиент для открытия инвентаря
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBlockEntityPacketId.Open,
                    data
                );

                // Открываем инвентарь для игрока
                byPlayer.InventoryManager.OpenInventory(inventory);
            }
            else
            {
                // Для клиента откроем инвентарь сразу
                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
                if (clientWorld.Player.InventoryManager != null)
                {
                    clientWorld.Player.InventoryManager.OpenInventory(inventory);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            if (packetid <= 1000)
            {
                inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                if (fromPlayer.InventoryManager != null)
                {
                    fromPlayer.InventoryManager.CloseInventory(inventory);
                }
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == (int)EnumBlockEntityPacketId.Open)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    TreeAttribute tree = new TreeAttribute();
                    tree.FromBytes(reader);
                    inventory.FromTreeAttributes(tree);
                    inventory.ResolveBlocksOrItems();

                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                    // Открытие GUI
                    if (clientWorld.Player.InventoryManager != null)
                    {
                        clientWorld.Player.InventoryManager.OpenInventory(inventory);
                    }
                }
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
                clientWorld.Player.InventoryManager.CloseInventory(inventory);
            }
        }
    }
}
