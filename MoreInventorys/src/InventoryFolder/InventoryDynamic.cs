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
        public int dynamicSlots = 0;
        //число слотов которые уже заняты контейнерами
        public int containerBlockSlotsActive = 0;

        //максимальное число контейнеров на стеллаже
        public int MaxContainerBlockSlots; 


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

        public InventoryDynamic(string inventoryID, int slots,ICoreAPI api)
            : base(slots, inventoryID, api)
        {
            this.slots = GenEmptySlots(slots);
            dynamicSlots = slots;
            baseWeight = 4f;
            ContainerSlots = new Dictionary<int, int[]>();
            LockContainerSlots = new object();
            MaxContainerBlockSlots = slots;

        }

        protected override ItemSlotDynamic NewSlot(int slotId)
        {
            if (onNewSlot != null)
            {
                return (ItemSlotDynamic)onNewSlot(slotId, this);
            }

            return new ItemSlotDynamic(this, slotId);
        }

        public bool RemoveSlots(int[] slots)
        {
            var slotsToRemove = new HashSet<int>(slots);

            // Фильтруем массив, оставляя только те элементы, индексы которых отсутствуют в slotsToRemove
            this.slots = this.slots.Where((value, index) => !slotsToRemove.Contains(index)).ToArray();

            return true;
        }



        public override void FromTreeAttributes(ITreeAttribute treeAttribute)
        {
            /*int num = slots.Length;
            slots = SlotsFromTreeAttributes(treeAttribute, slots);
            int amount = num - slots.Length;
            AddSlots(amount);*/
            slots = SlotsFromTreeAttributes(treeAttribute);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);

        }
    }
}
