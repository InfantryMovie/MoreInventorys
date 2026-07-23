// ItemSlotFixed.cs
using MoreInventorys.src.InventoryFolder;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace MoreInventorys.src
{
    /// <summary>
    /// Слот для фиксированного инвентаря стеллажа.
    /// Управляет доступностью слотов через проверку активности.
    /// </summary>
    public class ItemSlotFixed : ItemSlotSurvival
    {
        public int SlotId { get; }
        private InventoryFixed _inv;

        public ItemSlotFixed(InventoryBase inventory, int slotId) : base(inventory)
        {
            SlotId = slotId;
            if (inventory is InventoryFixed inv)
            {
                _inv = inv;

                // Закрашиваем слоты контейнеров серым
                if (_inv.IsContainerSlot(slotId))
                {
                    HexBackgroundColor = "#9f9f9f";
                }
            }
        }

        /// <summary>
        /// Проверяет, может ли слот принять предмет
        /// </summary>
        public override bool CanHold(ItemSlot sourceSlot)
        {
            // Если это не контейнерный слот и слот не активен — запрещаем
            if (!_inv.IsContainerSlot(SlotId) && !_inv.IsSlotActive(SlotId))
            {
                return false;
            }

            // Если это контейнерный слот и там уже есть контейнер — запрещаем
            if (_inv.IsContainerSlot(SlotId) && !Empty)
            {
                return false;
            }

            return base.CanHold(sourceSlot);
        }

        /// <summary>
        /// Проверяет, может ли кто-то взять предмет из слота
        /// </summary>
        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            if (_inv.IsTryPut) return base.CanTakeFrom(sourceSlot, priority);

            // Если слот не активен — запрещаем
            if (!_inv.IsSlotActive(SlotId))
            {
                return false;
            }

            // Если это контейнерный слот — запрещаем брать контейнер через интерфейс
            if (_inv.IsContainerSlot(SlotId))
            {
                return false;
            }

            return base.CanTakeFrom(sourceSlot, priority);
        }

        /// <summary>
        /// Проверяет, можно ли взять предмет из слота
        /// </summary>
        public override bool CanTake()
        {
            // Если слот не активен — запрещаем
            if (!_inv.IsSlotActive(SlotId))
            {
                return false;
            }

            // Если это контейнерный слот — запрещаем
            if (_inv.IsContainerSlot(SlotId))
            {
                return false;
            }

            return base.CanTake();
        }
    }
}