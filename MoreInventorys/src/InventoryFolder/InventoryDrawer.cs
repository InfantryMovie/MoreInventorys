using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace MoreInventorys.src.InventoryFolder
{
    public class InventoryDrawer : InventoryGeneric, ISlotProvider
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

        public InventoryDrawer(string inventoryID, int slots ,ICoreAPI api)
            : base(slots, inventoryID, api)
        {
            this.slots = GenEmptySlots(slots);
            baseWeight = 1f;

        }

        protected override ItemSlot NewSlot(int slotId)
        {
            return new StandardSlot(this);
        }

        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
        {
            if (sourceSlot.Empty) return true;

            ItemStack stack = sourceSlot.Itemstack;
            CollectibleObject collectible = stack.Collectible;
            if(collectible == null) return base.CanContain(sinkSlot, sourceSlot);
            string code = collectible.Code?.Path ?? "";
            string firstCodePart = collectible.Code?.FirstCodePart() ?? "";

            // ===== ПРОВЕРКА НА КЕРАМИКУ (всегда разрешаем) =====
            // Проверяем по материалу
            string material = collectible.Attributes?["material"]?.AsString();
            bool isCeramic = material == "ceramic";

            var isRackble = collectible.Attributes?.ToString().Contains("rackable");
            var isPlant = collectible.Attributes?.ToString().Contains("beeFeed");
            var isShelvable = collectible.Attributes?.ToString().Contains("shelvable");
            var isEatble = collectible.Attributes?.ToString().Contains("eat");
            if (code.Contains("armor")) isEatble = false;
            var isTool = collectible.CreativeInventoryTabs.Contains("tools");

            // Проверяем по коду предмета (если материал не указан)
            bool isClayItem = code.Contains("bowl") ||
                               code.Contains("crock") ||
                               code.Contains("crucible") ||
                               code.Contains("claypot") || code.Contains("jug") || code.Contains("flowerpot") || code.Contains("clayplanter") || code.Contains("wateringcan") ||
                               code.Contains("oillamp");

            bool isOtherAccesItem = code.Contains("inkandquil");
            bool isClutter = code.Contains("clutter");
            // Если это керамика — разрешаем (даже если это Block!)
            if (isCeramic || isClayItem || isOtherAccesItem || isPlant == true ||
                isEatble == true /*|| isClutter*/)
            {
                return true;
            }

            // ===== ЗАПРЕЩАЕМ ВСЕ БЛОКИ (кроме керамики, которую уже разрешили) =====
            if (collectible is Block)
            {
                return false;
            }

            // ===== ЗАПРЕЩАЕМ ИНСТРУМЕНТЫ И ОРУЖИЕ (по тегам) =====
            if (code.Contains("clothes") || code.Contains("armor"))
            {
                return false;
            }

            if (isShelvable == true) return true;
            if (isRackble == true || isTool)
            {
                return false;
            }

            if (collectible.Tool != null)
            {
                return false;
            }

            
            

            // ===== РАЗРЕШАЕМ ВСЁ ОСТАЛЬНОЕ =====
            return base.CanContain(sinkSlot, sourceSlot);
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
