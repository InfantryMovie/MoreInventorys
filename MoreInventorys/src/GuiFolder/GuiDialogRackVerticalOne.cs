using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockEntityFolder
{
    public class GuiDialogRackVerticalOne : GuiDialogBlockEntity
    {
        public GuiDialogRackVerticalOne(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (!base.IsDuplicate)
            {
                capi.World.Player.InventoryManager.OpenInventory(base.Inventory);
                base.Inventory.SlotModified += OnInventorySlotModified;
                SetupDialog();
            }
        }

        public void OnInventorySlotModified(int slotid)
        {
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupfortyeightslotdlg");
        }

        private void SetupDialog()
        {

            int[] slots = new int[48];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = i;
            }

            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == base.Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }
            ElementBounds mainBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 100.0);
            ElementBounds slotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 20.0, 9, 6);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(mainBounds);
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
            ClearComposers();
            base.SingleComposer = capi.Gui.CreateCompo("berackverticaloneslots" + base.BlockEntityPosition, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                .AddItemSlotGrid(base.Inventory, SendInvPacket, 9, slots, slotsBounds)
                .EndChildElements()
                .Compose();
            if (hoveredSlot != null)
            {
                base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
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
    }
}
