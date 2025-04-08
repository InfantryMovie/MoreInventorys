using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MoreInventorys.src.InventoryFolder
{
    public class InventoryFortyEight : InventoryBase, ISlotProvider
    {
        private ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        public override int Count => slots.Length;

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= Count)
                {
                    return null;
                }
                return slots[slotId];
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

        public InventoryFortyEight(string inventoryID, ICoreAPI api)
            : base(inventoryID, api)
        {
            slots = GenEmptySlots(48);
            baseWeight = 4f;

        }

        protected override ItemSlot NewSlot(int slotId)
        {
            return new StandardSlot(this);
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
