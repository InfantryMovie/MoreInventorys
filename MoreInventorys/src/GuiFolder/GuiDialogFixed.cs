// GuiDialogFixed.cs
using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.GuiFolder
{
    /// <summary>
    /// GUI для фиксированного инвентаря стеллажа.
    /// Отрисовывает только активные слоты.
    /// </summary>
    public class GuiDialogFixed : GuiDialogBlockEntity
    {
        private int[] activeSlots;
        private int activeSlotsCount;
        private InventoryFixed inventory;
        private BlockPos Pos;
        private int _selectedContainerSlot = -1;
        private List<int> _highlightedSlots = new List<int>();

        public GuiDialogFixed(int[] activeSlots, int activeSlotsCount, string dialogTitle, InventoryFixed inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (!base.IsDuplicate)
            {
                capi.World.Player.InventoryManager.OpenInventory(base.Inventory);
                inventory.SlotModified += OnInventorySlotModified;
                this.activeSlots = activeSlots;
                this.activeSlotsCount = activeSlotsCount;
                this.inventory = inventory;
                Pos = blockEntityPos;
                SetupDialog();
            }
        }

        public void OnInventorySlotModified(int slotid)
        {
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupdialog");
        }

        private void SetupDialog()
        {
            // Получаем слоты контейнеров
            int[] containerSlots = Enumerable.Range(0, inventory.MaxContainerBlockSlots).ToArray();

            // Получаем слоты предметов (все активные, кроме контейнерных)
            int[] itemSlots = activeSlots
                .Where(slot => slot >= inventory.MaxContainerBlockSlots)
                .ToArray();

            int cols = 9;
            if (itemSlots.Length > 0)
            {
                int rows = (int)Math.Ceiling((double)itemSlots.Length / cols);
                if (rows == 0 && activeSlotsCount > 0) rows = 1;

                const double slotHeight = 40;
                const double topPadding = 20;
                const double bottomPadding = 20;
                double fixedHeight = topPadding + (rows * slotHeight) + bottomPadding;
                double fixedWidth = 200;

                ElementBounds mainBounds = ElementBounds.Fixed(0.0, 0.0, fixedWidth, fixedHeight);
                ElementBounds containerSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 0.0, 20.0, 1, containerSlots.Length);
                ElementBounds slotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 75, 20.0, cols, rows);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren(mainBounds);

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                    .WithAlignment(EnumDialogArea.RightMiddle)
                    .WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

                ClearComposers();

                base.SingleComposer = capi.Gui
                    .CreateCompo("rackfixed" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, containerSlots, containerSlotsBounds, "containerSlots")
                    .AddItemSlotGrid(Inventory, SendInvPacket, cols, itemSlots, slotsBounds, "itemSlots")
                    .EndChildElements()
                    .Compose();
            }
            else
            {
                // Только контейнеры
                double fixedHeight = 20 + (containerSlots.Length * 40) + 20;
                double fixedWidth = 250;

                ElementBounds mainBounds = ElementBounds.Fixed(0.0, 0.0, fixedWidth, fixedHeight);
                ElementBounds containerSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 0.0, 20.0, 1, containerSlots.Length);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren(mainBounds);

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                    .WithAlignment(EnumDialogArea.RightMiddle)
                    .WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

                ClearComposers();

                base.SingleComposer = capi.Gui
                    .CreateCompo("rackfixed" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, containerSlots, containerSlotsBounds, "containerSlots")
                    .EndChildElements()
                    .Compose();
            }
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

        public override void OnGuiClosed()
        {
            base.Inventory.SlotModified -= OnInventorySlotModified;
            base.OnGuiClosed();
        }
    }
}