using MoreInventorys.src.BlockEntityFolder;
using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.GuiFolder
{
    public class GuiDialogDynamic : GuiDialogBlockEntity
    {
        //общее кол-во слотов с учетом контейнеров и предметов 
        int DynamicSlots;
        int MaxContainerBlockSlots;
        private int _selectedContainerSlot = -1; // -1 = ничего не выбрано
        private List<int> _highlightedSlots = new List<int>();
        bool isRack;

        BlockPos Pos { get; }
        public GuiDialogDynamic(int slots, string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (!base.IsDuplicate)
            {
                isRack = false;
                capi.World.Player.InventoryManager.OpenInventory(base.Inventory);
                inventory.SlotModified += OnInventorySlotModified;
                DynamicSlots = slots;
                Pos = blockEntityPos;
                if (inventory is InventoryDynamic inv)
                {
                    MaxContainerBlockSlots = inv.MaxContainerBlockSlots;
                    isRack = true;

                }
                SetupDialog();


            }
        }

        public void OnInventorySlotModified(int slotid)
        {
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupdynamicslotdlg");
        }


        private void SetupDialog()
        {
            //на случай если у нас есть двойные сундуки, чтобы не рисовать пустые слоты которые дополнительно занимает двойной сундук, убираем эти слоты!
            List<int> DoubleChestIndex = new List<int>();
            if (Inventory is InventoryDynamic inv)
            {
                if (inv.containerBlockSlotsActive > 0 && inv.Count > MaxContainerBlockSlots)
                {
                    if (inv.DoubleChestIndex.Count > 0)
                    {
                        DoubleChestIndex = inv.DoubleChestIndex;
                    }
                }
            }
            int cols = 0;
            int[] itemSlots = new int[0];

            int[] containerSlots = new int[0];

            // Вычисляем высоту динамически
            double fixedHeigh = 0;
            double fixedWidth = 0;

            if (MaxContainerBlockSlots == 3)
            {
                containerSlots = new int[3] { 0, 1, 2 };
            }
            if (MaxContainerBlockSlots == 2)
            {
                containerSlots = new int[2] { 0, 1};
            }
            if (MaxContainerBlockSlots == 4)
            {
                // Для 2х2 стеллажа (4 слота) - обрабатываем двойные сундуки
                if (DoubleChestIndex.Count == 0)
                {
                    containerSlots = new int[4] { 0, 1, 2, 3 };
                }
                else
                {
                    List<int> containerSlotsList = new List<int>() { 0, 1, 2, 3 };

                    foreach (var item in DoubleChestIndex)
                    {
                        // Удаляем правый слот двойного сундука (item+1)
                        // item - это левый слот, item+1 - правый
                        int rightSlot = item + 1;
                        if (containerSlotsList.Contains(rightSlot))
                        {
                            containerSlotsList.Remove(rightSlot);
                        }
                    }
                    containerSlots = containerSlotsList.ToArray();
                }
            }
            if (MaxContainerBlockSlots == 6)
            {
                if (DoubleChestIndex.Count == 0)
                {
                    containerSlots = new int[6] { 0, 1, 2, 3, 4, 5 };
                }
                else
                {
                    List<int> containerSlotsList = new List<int>() { 0, 1, 2, 3, 4, 5 };

                    foreach (var item in DoubleChestIndex)
                    {
                        // Удаляем правый слот двойного сундука (item+1)
                        int rightSlot = item + 1;
                        if (containerSlotsList.Contains(rightSlot))
                        {
                            containerSlotsList.Remove(rightSlot);
                        }
                    }
                    containerSlots = containerSlotsList.ToArray();
                }
            }

            if (DynamicSlots > MaxContainerBlockSlots)
            {
                //создаем слоты для предметов учитывая что "MaxContainerBlockSlots" слоты для контейнеров
                itemSlots = Enumerable.Range(MaxContainerBlockSlots, DynamicSlots - MaxContainerBlockSlots).ToArray();

                // Динамическое определение количества колонок в зависимости от количества слотов
                int totalItemSlots = itemSlots.Length;
                if (totalItemSlots <= 32)
                {
                    cols = 8;
                }
                else if (totalItemSlots <= 108)
                {
                    cols = 16;
                }
                else
                {
                    cols = 23;
                }

                fixedWidth = 200;

                // Вычисляем количество строк
                int rows = (int)Math.Ceiling((double)itemSlots.Length / cols);
                if (rows == 0 && DynamicSlots > 0) rows = 1;

                // Вычисляем высоту: отступ сверху 20 + (rows * высота слота) + отступ снизу
                const double slotHeight = 40;
                const double topPadding = 20;
                const double bottomPadding = 20;
                fixedHeigh = topPadding + (rows * slotHeight) + bottomPadding;
            }
            else
            {
                fixedWidth = 250;
                cols = 1;

                // Для вертикальных слотов контейнеров
                int visibleContainerSlots = containerSlots.Length;
                const double slotHeight = 40;
                const double topPadding = 20;
                const double bottomPadding = 20;
                fixedHeigh = topPadding + (visibleContainerSlots * slotHeight) + bottomPadding;
            }

            // Получаем текущий слот под мышью
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == base.Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }
            double offsetX = 0f;
            if (isRack) offsetX = 75f; // 75 Сдвиг основной сетки с предметами вправо от сетки с контейнерами


            // Основной контейнер
            ElementBounds mainBounds = ElementBounds.Fixed(0.0, 0.0, fixedWidth, fixedHeigh);

            // Левая часть — вертикальные слоты (для контейнеров)
            int visibleSlotCount = containerSlots.Length;
            ElementBounds containerSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 0.0, 20.0, 1, visibleSlotCount);

            ElementBounds slotsBounds = null;
            if (DynamicSlots > MaxContainerBlockSlots)
            {
                // отрисовываем правую часть только если слотов больше чем контейнеров
                int rows = (int)Math.Ceiling((double)itemSlots.Length / cols);
                if (rows == 0 && DynamicSlots > 0) rows = 1;
                slotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, offsetX, 20.0, cols, rows);
            }

            // Подложка
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(mainBounds);

            // Общие границы диалога
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

            // Очищаем старые GUI-элементы
            ClearComposers();

            // Создаём GUI

            if (slotsBounds != null)
            {
                if (isRack)
                {
                    base.SingleComposer = capi.Gui
                    .CreateCompo("blockentityinventoryberackhorizontalslots" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, containerSlots, containerSlotsBounds, "horizontalcontainerslots")
                    .AddItemSlotGrid(Inventory, SendInvPacket, cols, itemSlots, slotsBounds, "itemslots")
                    .EndChildElements()
                    .Compose();
                }
                else
                {
                    base.SingleComposer = capi.Gui
                    .CreateCompo("blockentityinventoryberackhorizontalslots" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    .AddItemSlotGrid(Inventory, SendInvPacket, cols, itemSlots, slotsBounds, "itemslots")
                    .EndChildElements()
                    .Compose();
                }
                
            }
            else
            {
                if (isRack)
                {
                    base.SingleComposer = capi.Gui
                    .CreateCompo("blockentityinventoryberackhorizontalslots" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, containerSlots, containerSlotsBounds, "horizontalcontainerslots")
                    .EndChildElements()
                    .Compose();
                }
                else
                {
                    base.SingleComposer = capi.Gui
                    .CreateCompo("blockentityinventoryberackhorizontalslots" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    .EndChildElements()
                    .Compose();
                }
                
            }

            //ПРИМЕНЯЕМ СОХРАНЁННОЕ ВЫДЕЛЕНИЕ
            if (_selectedContainerSlot >= 0)
            {
                ApplyHighlight(_selectedContainerSlot);
            }

            // Обновляем позицию курсора, если слот был наведен
            if (hoveredSlot != null)
            {
                base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }
        }

        //  ОБРАБОТЧИК КЛИКА МЫШИ
        //  ОБРАБОТЧИК КЛИКА МЫШИ
        //  ОБРАБОТЧИК КЛИКА МЫШИ
        public override void OnMouseDown(MouseEvent args)
        {
            base.OnMouseDown(args);

            if (args.Button != EnumMouseButton.Left) return;

            double mouseX = args.X;
            double mouseY = args.Y;

            var containerGrid = base.SingleComposer?.GetElement("horizontalcontainerslots") as GuiElementItemSlotGrid;
            if (containerGrid == null) return;

            var gridBounds = containerGrid.Bounds;
            if (!gridBounds.PointInside(mouseX, mouseY)) return;

            // Получаем список видимых слотов
            List<int> visibleSlots = new List<int>();
            for (int i = 0; i < MaxContainerBlockSlots; i++)
            {
                if (i % 2 == 1 && Inventory is InventoryDynamic inv && inv.DoubleChestIndex.Contains(i - 1))
                    continue;
                visibleSlots.Add(i);
            }

            //capi.Logger.Debug($"[GUI] visibleSlots: {string.Join(", ", visibleSlots)}");

            if (visibleSlots.Count == 0) return;

            double slotHeight = gridBounds.InnerHeight / visibleSlots.Count;
            double relativeY = mouseY - gridBounds.absY;
            int visibleIndex = (int)(relativeY / slotHeight);

            //capi.Logger.Debug($"[GUI] mouseY: {mouseY}, gridBounds.absY: {gridBounds.absY}, relativeY: {relativeY}, slotHeight: {slotHeight}, visibleIndex: {visibleIndex}");

            if (visibleIndex >= 0 && visibleIndex < visibleSlots.Count)
            {
                // Получаем реальный ID слота из visibleSlots
                int actualSlotId = visibleSlots[visibleIndex];
                //capi.Logger.Debug($"[GUI] Клик по видимому индексу {visibleIndex} -> реальный слот {actualSlotId}");

                // Проверяем наличие контейнера в этом слоте
                bool isEmpty = Inventory[actualSlotId].Empty;
                //capi.Logger.Debug($"[GUI] Слот {actualSlotId} пуст: {isEmpty}");

                if (!isEmpty)
                {
                    // Проверяем, есть ли контейнер в ContainerSlots
                    if (Inventory is InventoryDynamic inv)
                    {
                        bool hasKey = inv.ContainerSlots.ContainsKey(actualSlotId);
                        //capi.Logger.Debug($"[GUI] ContainerSlots содержит ключ {actualSlotId}: {hasKey}");
                        if (hasKey)
                        {
                            int[] slots = inv.ContainerSlots[actualSlotId];
                            //capi.Logger.Debug($"[GUI] Слоты для контейнера {actualSlotId}: [{string.Join(", ", slots)}]");
                        }
                        else
                        {
                            // Показываем все ключи в ContainerSlots
                            //capi.Logger.Debug($"[GUI] Все ключи в ContainerSlots: [{string.Join(", ", inv.ContainerSlots.Keys)}]");
                        }
                    }

                    // Используем реальный slotId для выделения
                    HandleContainerSlotClick(actualSlotId);
                }
            }
        }

        private int[] GetVisibleContainerSlots()
        {
            List<int> visibleSlots = new List<int>();

            for (int i = 0; i < MaxContainerBlockSlots; i++)
            {
                // Пропускаем правые слоты двойных сундуков
                if (i % 2 == 1)
                {
                    int leftSlot = i - 1;
                    if (Inventory is InventoryDynamic inv && inv.DoubleChestIndex.Contains(leftSlot))
                    {
                        continue; // Пропускаем правый слот двойного сундука
                    }
                }
                visibleSlots.Add(i);
            }

            return visibleSlots.ToArray();
        }


        private void HandleContainerSlotClick(int slotId)
        {
            //capi.Logger.Debug($"[GUI] HandleContainerSlotClick вызван для slotId: {slotId}, текущий _selectedContainerSlot: {_selectedContainerSlot}");

            // Если кликнули по тому же слоту - снимаем выделение
            if (_selectedContainerSlot == slotId)
            {
                //capi.Logger.Debug($"[GUI] Клик по тому же слоту {slotId}, снимаем выделение");
                ClearHighlight();
                return;
            }

            // Выделяем слоты этого контейнера
            //capi.Logger.Debug($"[GUI] Применяем выделение для слота {slotId}");
            ApplyHighlight(slotId);
            _selectedContainerSlot = slotId;
        }

        private void ApplyHighlight(int containerSlotId)
        {
            //capi.Logger.Debug($"[GUI] ApplyHighlight вызван для containerSlotId: {containerSlotId}");

            ClearHighlight();

            if (Inventory is InventoryDynamic inv)
            {
                // Показываем все ключи в ContainerSlots
                //capi.Logger.Debug($"[GUI] ContainerSlots ключи: [{string.Join(", ", inv.ContainerSlots.Keys)}]");

                // Проверяем, есть ли контейнер в ContainerSlots по этому ключу
                if (inv.ContainerSlots.ContainsKey(containerSlotId))
                {
                    int[] itemSlots = inv.ContainerSlots[containerSlotId];
                    //capi.Logger.Debug($"[GUI] Найдены слоты для контейнера {containerSlotId}: [{string.Join(", ", itemSlots)}]");

                    _highlightedSlots.AddRange(itemSlots);

                    foreach (int slotId in _highlightedSlots)
                    {
                        if (Inventory[slotId] is ItemSlotDynamic slot)
                        {
                            slot.HexBackgroundColor = "#4CAF50";
                        }
                    }

                    SetupDialog();
                }
                else
                {
                    //capi.Logger.Debug($"[GUI] Ключ {containerSlotId} НЕ НАЙДЕН в ContainerSlots!");
                    //capi.Logger.Debug($"[GUI] Доступные ключи: [{string.Join(", ", inv.ContainerSlots.Keys)}]");
                }
            }
        }

        private void ClearHighlight()
        {
            foreach (int slotId in _highlightedSlots)
            {
                if (Inventory[slotId] is ItemSlotDynamic slot)
                {
                    slot.HexBackgroundColor = null;
                }
            }

            _highlightedSlots.Clear();
            _selectedContainerSlot = -1;

            SetupDialog();
        }

        public override void OnGuiClosed()
        {
            ClearHighlight();
            base.Inventory.SlotModified -= OnInventorySlotModified;
            base.OnGuiClosed();
        }




        public override void OnKeyPress(KeyEvent args)
        {
            //клавиша R/К   
            if (args.KeyCode == 114 || args.KeyCode == 1082)
            {

            }
            base.OnKeyPress(args);
        }


        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, p);
        }

        private void OnTitleBarClose()
        {
            TryClose();
            base.Inventory.SlotModified -= OnInventorySlotModified;
        }

        public override bool OnEscapePressed()
        {
            base.Inventory.SlotModified -= OnInventorySlotModified;
            return base.OnEscapePressed();
        }
    }
}