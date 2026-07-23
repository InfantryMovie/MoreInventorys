using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MoreInventorys.src.InventoryFolder
{
    public class InventoryFixed : InventoryGeneric
    {
        public const int MAX_SLOTS = 9999;

        private HashSet<int> activeSlots = new HashSet<int>();

        public int MaxContainerBlockSlots { get; private set; }
        public int containerBlockSlotsActive = 0;

        public int ActiveSlotsCount => activeSlots.Count;
        public int[] ActiveSlots => activeSlots.ToArray();
        public List<int> DoubleChestIndex { get; set; } = new List<int>();
        public bool IsTryPut = false;

        public InventoryFixed(string inventoryID, int maxContainerBlockSlots, ICoreAPI api)
            : base(MAX_SLOTS, inventoryID, api)
        {
            MaxContainerBlockSlots = maxContainerBlockSlots;

            for (int i = 0; i < MaxContainerBlockSlots; i++)
            {
                activeSlots.Add(i);
            }
        }

        protected override ItemSlot NewSlot(int slotId)
        {
            return new ItemSlotFixed(this, slotId);
        }

        public void AddContainerSlots(int containerSlotId, int quantitySlots)
        {
            int startSlot = activeSlots.Count > 0 ? activeSlots.Max() + 1 : MaxContainerBlockSlots;

            for (int i = 0; i < quantitySlots; i++)
            {
                int slotId = startSlot + i;
                if (slotId < MAX_SLOTS)
                {
                    activeSlots.Add(slotId);
                }
            }
        }

        public bool IsSlotActive(int slotId)
        {
            return activeSlots.Contains(slotId);
        }

        public bool IsContainerSlot(int slotId)
        {
            return slotId < MaxContainerBlockSlots;
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            base.FromTreeAttributes(tree);

            activeSlots.Clear();

            // ✅ ВСЕГДА добавляем слоты контейнеров (0 и 1)
            for (int i = 0; i < MaxContainerBlockSlots; i++)
            {
                activeSlots.Add(i);
            }

            // ✅ Восстанавливаем остальные активные слоты
            var activeSlotsAttr = tree["activeSlots"] as IntArrayAttribute;
            if (activeSlotsAttr != null)
            {
                foreach (int slotId in activeSlotsAttr.value)
                {
                    if (slotId >= MaxContainerBlockSlots)
                    {
                        activeSlots.Add(slotId);
                    }
                }
            }

            containerBlockSlotsActive = tree.GetInt("containerBlockSlotsActive", 0);

            DoubleChestIndex = new List<int>();
            var doubleChestTree = tree["doubleChestIndex"] as IntArrayAttribute;
            if (doubleChestTree != null)
            {
                DoubleChestIndex.AddRange(doubleChestTree.value);
            }

            // ✅ Лог для отладки
            if (Api != null)
            {
                Api.Logger.Notification($"InventoryFixed: Loaded {activeSlots.Count} active slots");
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            // ✅ Сохраняем ВСЕ активные слоты
            tree["activeSlots"] = new IntArrayAttribute(activeSlots.ToArray());
            tree.SetInt("containerBlockSlotsActive", containerBlockSlotsActive);
            tree["doubleChestIndex"] = new IntArrayAttribute(DoubleChestIndex?.ToArray() ?? new int[0]);
        }
    }
}