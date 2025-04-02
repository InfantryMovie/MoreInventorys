using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace MoreInventorys.src
{
    internal class StandardSlot : ItemSlotSurvival
    {
        public StandardSlot(InventoryBase inventory)
            : base(inventory)
        {
        }

        private bool IsTool(ItemSlot itemSlot)
        {
            if (itemSlot.Itemstack != null)
            {
                return itemSlot.Itemstack.Collectible.Tool.HasValue;
            }
            return false;
        }

        public override bool CanHold(ItemSlot itemstackFromSourceSlot)
        {
            if (!IsTool(itemstackFromSourceSlot))
            {
                return base.CanHold(itemstackFromSourceSlot);
            }
            return false;
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            if (!IsTool(sourceSlot))
            {
                return base.CanTakeFrom(sourceSlot, EnumMergePriority.AutoMerge);
            }
            return false;
        }
    }
}
