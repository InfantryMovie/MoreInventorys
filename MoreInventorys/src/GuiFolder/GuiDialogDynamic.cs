using MoreInventorys.src.BlockEntityFolder;
using MoreInventorys.src.InventoryFolder;
using System;
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
        
        BlockPos Pos { get; }
        public GuiDialogDynamic(int slots, string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (!base.IsDuplicate)
            {
                capi.World.Player.InventoryManager.OpenInventory(base.Inventory);
                inventory.SlotModified += OnInventorySlotModified;
                DynamicSlots = slots;
                Pos = blockEntityPos;
                if(inventory is InventoryDynamic inv)
                {
                    MaxContainerBlockSlots = inv.MaxContainerBlockSlots;

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
                if(inv.containerBlockSlotsActive > 0 && inv.Count > MaxContainerBlockSlots)
                {
                   if(inv.DoubleChestIndex.Count > 0)
                   {
                        DoubleChestIndex = inv.DoubleChestIndex;
                   }
                }
            }
            int cols = 0;
            int[] itemSlots = new int[0];

            int[] containerSlots = new int[0];
            double fixedHeigh = 100;

            if(MaxContainerBlockSlots == 3)
            {
                containerSlots = new int[3] {0,1,2};

            }
            if (MaxContainerBlockSlots == 4)
            {
                containerSlots = new int[4] { 0, 1, 2, 3};
            }

            if (MaxContainerBlockSlots == 6)
            {
                if(DoubleChestIndex.Count == 0)
                {
                    containerSlots = new int[6] { 0, 1, 2, 3, 4, 5 };
                }
                else
                {
                    int containerSlotsCount = 6;
                    containerSlotsCount -= DoubleChestIndex.Count;
                    List<int> containerSlotsList = new List<int>() { 0, 1, 2, 3, 4, 5 };

                    foreach (var item in DoubleChestIndex)
                    {
                        containerSlotsList.Remove(item+1);
                    }
                    containerSlots = containerSlotsList.ToArray();
                }

                

               
            }

            double fixedWidth = 0;

            if (DynamicSlots > MaxContainerBlockSlots)
            {
                //создаем слоты для предметов учитывая что "MaxContainerBlockSlots" слоты для контейнеров
                itemSlots = Enumerable.Range(MaxContainerBlockSlots, DynamicSlots - MaxContainerBlockSlots).ToArray();
                cols = 9;
                fixedWidth = 200;
            }
            else
            {
                fixedWidth = 250;
                cols = 1;
            }


            int rows = (int)Math.Ceiling((double)DynamicSlots / cols);

            if (rows == 0 && DynamicSlots > 0) rows = 1;


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

            double offsetX = 75; // Сдвиг основной сетки с предметами вправо от сетки с контейнерами


            // Основной контейнер
            ElementBounds mainBounds = ElementBounds.Fixed(0.0, 0.0, fixedWidth, fixedHeigh);

            // Левая часть — вертикальные слоты (для контейнеров)
            ElementBounds containerSlotsBounds = ElementStdBounds.SlotGrid(EnumDialogArea.LeftFixed, 0.0, 20.0, 1, MaxContainerBlockSlots);

            ElementBounds slotsBounds = null;
            if (DynamicSlots > MaxContainerBlockSlots)
            {
                // отрисовываем правую часть только если слотов больше чем контейнеров
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

                base.SingleComposer = capi.Gui
                    .CreateCompo("berackhorizontalslots" + base.BlockEntityPosition, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                    .BeginChildElements(bgBounds)
                    // Добавляем вертикальные слоты слева (для контейнеров)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, containerSlots, containerSlotsBounds, "horizontalcontainerslots")
                    // Добавляем основную сетку
                    .AddItemSlotGrid(Inventory, SendInvPacket, cols, itemSlots, slotsBounds, "itemslots")

                    .EndChildElements()
                    .Compose();

            }
            else
            {
                base.SingleComposer = capi.Gui
                .CreateCompo("berackhorizontalslots" + base.BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                // Добавляем вертикальные слоты слева (для контейнеров)
                .AddItemSlotGrid(Inventory, SendInvPacket, 1, containerSlots, containerSlotsBounds, "horizontalcontainerslots")
                .EndChildElements()
                .Compose();
            }

            // Обновляем позицию курсора, если слот был наведен
            if (hoveredSlot != null)
            {
                base.SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }
        }

        public override void OnKeyPress(KeyEvent args)
        {
            //клавиша R/К   
            if(args.KeyCode == 114 || args.KeyCode == 1082 )
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
