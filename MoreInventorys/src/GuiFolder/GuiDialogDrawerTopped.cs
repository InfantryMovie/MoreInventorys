using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.GuiFolder
{
    public class GuiDialogDrawerTopped : GuiDialogBlockEntity
    {
        private InventoryDrawer _inventory;
        private BlockPos _pos;

        // Определяем группы слотов
        private int[] topSlots = { 0, 1, 2, 3 };                    // Верх (4 слота)
        private int[] middleSlots = { 4, 5, 6, 7, 8, 9, 10, 11 };   // Средний ящик (8 слотов)
        private int[] bottomSlots = { 12, 13, 14, 15 };              // Нижняя ниша (4 слота)

        public GuiDialogDrawerTopped(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            _inventory = inventory as InventoryDrawer;
            _pos = blockEntityPos;

            if (!IsDuplicate)
            {
                capi.World.Player.InventoryManager.OpenInventory(inventory);
                inventory.SlotModified += OnInventorySlotModified;
                SetupDialog();
            }
        }

        private void OnInventorySlotModified(int slotid)
        {
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupdrawertoppeddlg");
        }

        private void SetupDialog()
        {
            // Сохраняем состояние ховера
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }

            // ===== РАЗМЕРЫ =====
            const int slotSize = 40;
            const int gap = 6;
            const int sectionGap = slotSize + gap; // Отступ между секциями = размер слота + отступ
            const int padding = 20;
            const int leftOffset = 10; // Уменьшенный отступ слева для компенсации внутреннего отступа
            const int titleBarHeight = 30;

            // Ширина: 4 слота + отступы между ними + отступы по краям
            int width = padding + (slotSize * 4) + (gap * 3) + padding;

            // Высота: заголовок + отступ сверху + верх (1 ряд) + отступ между секциями 
            // + средний (2 ряда) + отступ между секциями + низ (1 ряд)
            int height = titleBarHeight + padding +
                         (slotSize + gap) + sectionGap +
                         (slotSize + gap) * 2 + sectionGap;

            // ===== ОСНОВНЫЕ ГРАНИЦЫ =====
            ElementBounds mainBounds = ElementBounds.Fixed(0, 0, width, height);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(mainBounds);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

            // ===== СОЗДАЁМ КОМПОЗИТОР =====
            var composer = capi.Gui
                .CreateCompo("drawertoppedgui" + _pos, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds);

            double yOffset = padding;

            // ===== 1. ВЕРХНИЕ СЛОТЫ (4) =====
            ElementBounds topBounds = ElementStdBounds.SlotGrid(
                EnumDialogArea.None,
                leftOffset,  // Уменьшенный отступ слева
                yOffset,
                4,   // колонки
                1    // строки
            );
            composer.AddItemSlotGrid(Inventory, SendInvPacket, 4, topSlots, topBounds, "topslots");

            // ===== ОТСТУП МЕЖДУ СЕКЦИЯМИ =====
            yOffset += slotSize + gap + sectionGap;

            // ===== 2. СРЕДНИЕ СЛОТЫ (8) =====
            ElementBounds middleBounds = ElementStdBounds.SlotGrid(
                EnumDialogArea.None,
                leftOffset,  // Уменьшенный отступ слева
                yOffset,
                4,   // колонки
                2    // строки
            );
            composer.AddItemSlotGrid(Inventory, SendInvPacket, 4, middleSlots, middleBounds, "middleslots");

            // ===== ОТСТУП МЕЖДУ СЕКЦИЯМИ =====
            yOffset += (slotSize + gap) * 2 + sectionGap;

            // ===== 3. НИЖНИЕ СЛОТЫ (4) =====
            ElementBounds bottomBounds = ElementStdBounds.SlotGrid(
                EnumDialogArea.None,
                leftOffset,  // Уменьшенный отступ слева
                yOffset,
                4,   // колонки
                1    // строки
            );
            composer.AddItemSlotGrid(Inventory, SendInvPacket, 4, bottomSlots, bottomBounds, "bottomslots");

            // ===== ЗАВЕРШАЕМ =====
            composer.EndChildElements().Compose();
            SingleComposer = composer;

            // Восстанавливаем ховер, если был
            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }
        }

        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(_pos.X, _pos.Y, _pos.Z, p);
        }

        private void OnTitleBarClose()
        {
            TryClose();
            Inventory.SlotModified -= OnInventorySlotModified;
        }

        public override bool OnEscapePressed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;
            return base.OnEscapePressed();
        }

        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;
            base.OnGuiClosed();
        }
    }
}