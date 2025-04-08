using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace MoreInventorys.src.InventoryFolder
{
    public class InventoryDynamic : InventoryGeneric
    {
        //тут храним инфу по слотам которые дал текущий контейнер, ключ - блок, значение массив слотов с индексами == SlotId
        public Dictionary<int, int[]> ContainerSlots { get; set; }
        public object LockContainerSlots { get; set; }

        private NewSlotDelegate onNewSlot;
        public override int Count => slots.Length;

        //число слотов внутренних хранилищ стеллажа
        public int dynamicSlots = 3;
        //число слотов которые уже заняты контейнерами на стеллаже, 3 максимум, 0 = нет ни 1-го контейнера
        public int containerBlockSlotsActive = 0;

        public new ItemSlotDynamic this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= Count)
                {
                    return null;
                }
                return (ItemSlotDynamic)slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                slots[slotId] = value;
            }
        }


        public InventoryDynamic(string inventoryID, int slots, ICoreAPI api)
            : base(slots, inventoryID, api)
        {
            this.slots = GenEmptySlots(slots);
            baseWeight = 4f;
            ContainerSlots = new Dictionary<int, int[]>();
            LockContainerSlots = new object();

        }

        public bool RemoveSlots(int[] slots)
        {
            // Преобразуем slots в HashSet для ускорения поиска
            var slotsToRemove = new HashSet<int>(slots);

            // Фильтруем массив, оставляя только те элементы, индексы которых отсутствуют в slotsToRemove
            this.slots = this.slots.Where((value, index) => !slotsToRemove.Contains(index)).ToArray();

            return true;
        }

      
        

        protected override ItemSlotDynamic NewSlot(int slotId)
        {
            if (onNewSlot != null)
            {
                return (ItemSlotDynamic)onNewSlot(slotId, this);
            }

            return new ItemSlotDynamic(this, slotId);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree);


        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);

        }
    }
}
