using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.GuiFolder
{
    /// <summary>
    /// Типы тумбочек для отображения GUI
    /// </summary>
    public enum DrawerType
    {
        /// <summary>
        /// Тумба с ящиком и нишей (4 сверху, 8 ящик, 4 снизу)
        /// </summary>
        DrawerWithNiche,

        /// <summary>
        /// Тумба с открытой полкой и дверцей (8 сверху, 8 снизу)
        /// </summary>
        OpenShelfWithDoor,
        LargeOpenShelfDoorCabinet

        // ============================================================
        //    КАК ДОБАВИТЬ НОВЫЙ ТИП ТУМБОЧКИ:
        // 1. Добавь сюда новый enum (например, "MyNewDrawerType")
        // 2. В методе GetSlotLayout() добавь новый case
        // 3. В SetupDialog() добавь обработку нового типа
        // 4. В BEDrawerTopped / BEOpenShelfDoorCabinet передавай новый тип
        // ============================================================
    }

    public class GuiDialogDrawers : GuiDialogBlockEntity
    {
        private InventoryDrawer _inventory;
        private BlockPos _pos;
        private DrawerType _drawerType;
        private int slotCount;

        // Определяем группы слотов
        private int[] topSlots;
        private int[] middleSlots;
        private int[] bottomSlots;

        public GuiDialogDrawers(
            string dialogTitle,
            InventoryBase inventory,
            BlockPos blockEntityPos,
            ICoreClientAPI capi,
            DrawerType drawerType = DrawerType.DrawerWithNiche)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            _inventory = inventory as InventoryDrawer;
            _pos = blockEntityPos;
            _drawerType = drawerType;

            // Определяем layout слотов в зависимости от типа
            GetSlotLayout();

            if (!IsDuplicate)
            {
                capi.World.Player.InventoryManager.OpenInventory(inventory);
                inventory.SlotModified += OnInventorySlotModified;
                SetupDialog();
            }
        }

        /// <summary>
        /// Определяет layout слотов в зависимости от типа тумбочки
        /// </summary>
        private void GetSlotLayout()
        {
            switch (_drawerType)
            {
                case DrawerType.DrawerWithNiche:
                    // Тумба с ящиком и нишей: 4 сверху, 8 ящик, 4 снизу
                    topSlots = new int[] { 0, 1, 2, 3 };
                    middleSlots = new int[] { 4, 5, 6, 7, 8, 9, 10, 11 };
                    bottomSlots = new int[] { 12, 13, 14, 15 };
                    break;

                case DrawerType.OpenShelfWithDoor:
                    // Тумба с открытой полкой и дверцей: 8 сверху (4+4), 8 снизу (4+4)
                    topSlots = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                    middleSlots = null; // Нет средних слотов
                    bottomSlots = new int[] { 8, 9, 10, 11, 12, 13, 14, 15 };
                    break;

                case DrawerType.LargeOpenShelfDoorCabinet:
                    
                    topSlots = new int[] { 0, 1, 2, 3, 4, 5, 6, 7,8,9,10,11,
                                           12,13,14,15,16,17,18,19,20,21,22,23};
                    middleSlots = null; // Нет средних слотов
                    bottomSlots = new int[] {24,25,26,27,28,29,30,31,32,33,34,35,
                                             36,37,38,39,40,41,42,43,44,45,46,47};
                    break;


            }
        }

        private void OnInventorySlotModified(int slotid)
        {
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupdrawersdlg");
        }

        /// <summary>
        /// Проверяет, нужно ли рисовать пустые слоты-разделители
        /// </summary>
        private bool HasGapBetweenSections()
        {
            // Если есть и верхние, и нижние слоты — нужен разделитель
            return topSlots != null && topSlots.Length > 0 &&
                   bottomSlots != null && bottomSlots.Length > 0;
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
            const int sectionGap = slotSize + gap; // Отступ между секциями
            const int padding = 20;
            const int leftOffset = 10;
            const int titleBarHeight = 30;

            // Определяем, сколько рядов нужно
            int rowCount = 4;
            if (_drawerType == DrawerType.LargeOpenShelfDoorCabinet) rowCount = 6;
            int topRows = topSlots != null ? (int)Math.Ceiling((double)topSlots.Length / rowCount) : 0;
            int middleRows = middleSlots != null ? (int)Math.Ceiling((double)middleSlots.Length / rowCount) : 0;
            int bottomRows = bottomSlots != null ? (int)Math.Ceiling((double)bottomSlots.Length / rowCount) : 0;

            // Проверяем, нужен ли разделитель между секциями
            bool hasGap = HasGapBetweenSections();

            // Ширина: 4 слота + отступы между ними + отступы по краям
            int width = padding + (slotSize * rowCount) + (gap * 3) + padding;
            //width += 10;

            // Высота: заголовок + отступ сверху + все ряды + отступы между секциями
            int height = titleBarHeight + padding;

            // Верхние слоты
            if (topRows > 0)
            {
                height += (slotSize + gap) * topRows;
                if (hasGap || middleSlots != null) height += sectionGap;
            }

            // Средние слоты (если есть)
            if (middleRows > 0)
            {
                height += (slotSize + gap) * middleRows;
                if (hasGap) height += sectionGap;
            }


            // ===== ОСНОВНЫЕ ГРАНИЦЫ =====
            ElementBounds mainBounds = ElementBounds.Fixed(0, 0, width, height);
            // Создаём bgBounds без нижнего отступа
            ElementBounds bgBounds = ElementBounds.Fill;
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(mainBounds);
            // Вручную устанавливаем отступы: лево, право, верх — как обычно, низ — 0
            bgBounds.fixedPaddingX = GuiStyle.ElementToDialogPadding;
            bgBounds.fixedPaddingY = GuiStyle.ElementToDialogPadding;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

            // ===== СОЗДАЁМ КОМПОЗИТОР =====
            var composer = capi.Gui
                .CreateCompo("drawersgui" + _pos, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds);

            double yOffset = padding;

            // ===== 1. ВЕРХНИЕ СЛОТЫ =====
            if (topSlots != null && topSlots.Length > 0)
            {
                int cols = Math.Min(rowCount, topSlots.Length);
                int rows = (int)Math.Ceiling((double)topSlots.Length / rowCount);

                ElementBounds topBounds = ElementStdBounds.SlotGrid(
                    EnumDialogArea.None,
                    leftOffset,
                    yOffset,
                    cols,
                    rows
                );
                composer.AddItemSlotGrid(Inventory, SendInvPacket, cols, topSlots, topBounds, "topslots");

                yOffset += (slotSize + gap) * rows;

                // Добавляем разделитель, если есть ещё секции
                if (HasGapBetweenSections() || middleSlots != null)
                {
                    yOffset += sectionGap;
                }
            }

            // ===== 2. СРЕДНИЕ СЛОТЫ (если есть) =====
            if (middleSlots != null && middleSlots.Length > 0)
            {
                int cols = Math.Min(rowCount, middleSlots.Length);
                int rows = (int)Math.Ceiling((double)middleSlots.Length / rowCount);

                ElementBounds middleBounds = ElementStdBounds.SlotGrid(
                    EnumDialogArea.None,
                    leftOffset,
                    yOffset,
                    cols,
                    rows
                );
                composer.AddItemSlotGrid(Inventory, SendInvPacket, cols, middleSlots, middleBounds, "middleslots");

                yOffset += (slotSize + gap) * rows;

                if (HasGapBetweenSections())
                {
                    yOffset += sectionGap;
                }
            }

            // ===== 3. НИЖНИЕ СЛОТЫ =====
            if (bottomSlots != null && bottomSlots.Length > 0)
            {
                int cols = Math.Min(rowCount, bottomSlots.Length);
                int rows = (int)Math.Ceiling((double)bottomSlots.Length / rowCount);

                ElementBounds bottomBounds = ElementStdBounds.SlotGrid(
                    EnumDialogArea.None,
                    leftOffset,
                    yOffset,
                    cols,
                    rows
                );
                composer.AddItemSlotGrid(Inventory, SendInvPacket, cols, bottomSlots, bottomBounds, "bottomslots");
            }

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