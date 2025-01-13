using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src
{
    // Класс GUI для блока полки
    public class ShelfBlockEntityGui : GuiDialog
    {
        private InventoryBase inventory;
        private BlockPos blockPos;

        public override string ToggleKeyCombinationCode => throw new NotImplementedException();

        // Конструктор окна GUI
        public ShelfBlockEntityGui(string dialogTitle, InventoryBase inventory, BlockPos blockPos, ICoreClientAPI capi)
            : base(capi)
        {
            this.inventory = inventory;
            this.blockPos = blockPos;

            // Составляем интерфейс
            ComposeDialog();
        }

        // Составляем интерфейс для полки
        private void ComposeDialog()
        {
            // Определяем размеры окна и расположение элементов
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(0, 0);

            ElementBounds slotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, 0, 0, 4, 2);

            // Создаем компоновку GUI
            SingleComposer = capi.Gui
                .CreateCompo("shelfGui", dialogBounds)
                .AddShadedDialogBG(dialogBounds)
                .AddDialogTitleBar("МояПолка", OnTitleBarClose)
                .AddItemSlotGrid(inventory, _ => SendInvPacket(), 4, slotBounds, "shelfSlots") 
                .Compose();
        }

        // Закрытие окна
        private void OnTitleBarClose()
        {
            TryClose();
        }

        // Обновление окна, если данные инвентаря изменились
        public override bool TryOpen()
        {
            if (IsOpened()) return false;
            return base.TryOpen();
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();

            // Создаем дерево атрибутов для хранения данных инвентаря
            ITreeAttribute tree = new TreeAttribute();
            inventory.ToTreeAttributes(tree);

            // Сохраняем изменения, если игрок взаимодействовал с инвентарем
            capi.Network.SendBlockEntityPacket(blockPos, 1337, tree);
        }

        private void SendInvPacket()
        {
            // Создаем дерево атрибутов для отправки инвентаря
            ITreeAttribute tree = new TreeAttribute();
            inventory.ToTreeAttributes(tree);

            // Отправляем данные на сервер
            capi.Network.SendBlockEntityPacket(blockPos, 1337, tree);
        }
    }
}
