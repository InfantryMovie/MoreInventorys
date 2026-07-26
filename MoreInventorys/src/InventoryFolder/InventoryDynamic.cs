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

        public Dictionary<int, string> ContainerTypes { get; set; } = new Dictionary<int, string>();

        //список двойных сундуков и индексы которые они занимают на стеллаже (нужно чтобы убрать по индексу в ГУИ лишние слоты)
        public List<int> DoubleChestIndex { get; set; }
        public object LockContainerSlots { get; set; }

        private NewSlotDelegate onNewSlot;
        public override int Count => slots.Length;

        //число слотов внутренних хранилищ стеллажа
        public int dynamicSlots = 0;
        //число слотов которые уже заняты контейнерами
        public int containerBlockSlotsActive = 0;

        //максимальное число контейнеров на стеллаже
        public int MaxContainerBlockSlots;

        public bool IsTryPut = false;


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
            DoubleChestIndex = new List<int>();
            this.OnAcquireTransitionSpeed += OnAcquireTransitionSpeedHandler;

        }

        private float OnAcquireTransitionSpeedHandler(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            // Обрабатываем только порчу и созревание
            if (transType != EnumTransitionType.Perish && transType != EnumTransitionType.Ripen)
                return baseMul;

            if (stack == null || stack.StackSize <= 0)
                return baseMul;

            // Находим слот, для которого вызвано событие
            int slotId = -1;
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i]?.Itemstack != null && this[i].Itemstack.Equals(stack))
                {
                    slotId = i;
                    break;
                }
            }

            if (slotId == -1)
                return baseMul;

            int containerSlotId = GetContainerSlotIdForSlot(slotId);

            if (containerSlotId == -1)
                return baseMul;

            // Получаем тип контейнера
            if (ContainerTypes == null || !ContainerTypes.TryGetValue(containerSlotId, out string containerType))
                return baseMul;

            // Логика для Storage Vessel
            if (containerType.Contains("storagevessel") ||
                containerType.Contains("vessel") ||
                containerType.Contains("storage"))
            {
                if (stack.Collectible != null)
                {
                    string foodCategory = GetFoodCategory(stack);

                    if (foodCategory == "grain")
                    {
                        return 0.5f * baseMul;
                    }
                    else if (foodCategory == "vegetable")
                    {
                        return 0.75f * baseMul;
                    }
                    else if (foodCategory == "fruit")
                    {
                        return 0.8f * baseMul;
                    }
                }
            }

            return baseMul;
        }

        private string GetFoodCategory(ItemStack stack)
        {
            if (stack?.Collectible == null) return "";

            // Проверяем через переходные свойства
            var props = stack.Collectible.GetTransitionableProperties(Api.World, stack, null);
            if (props != null)
            {
                foreach (var prop in props)
                {
                    if (prop.Type == EnumTransitionType.Perish)
                    {
                        // У зерна и овощей разные параметры порчи
                        // Можно определить по длительности хранения или по коду предмета
                        string code = stack.Collectible.Code?.Path ?? "";

                        if (code.Contains("grain") || code.Contains("flax") || code.Contains("rye") ||
                            code.Contains("spelt") || code.Contains("rice") || code.Contains("cassava"))
                        {
                            return "grain";
                        }
                        else if (code.Contains("vegetable") || code.Contains("onion") || code.Contains("cabbage") ||
                                 code.Contains("carrot") || code.Contains("turnip") || code.Contains("pumpkin"))
                        {
                            return "vegetable";
                        }
                        else if (code.Contains("fruit") || code.Contains("apple") || code.Contains("berry") ||
                                 code.Contains("saguaro") || code.Contains("pomegranate"))
                        {
                            return "fruit";
                        }
                    }
                }
            }

            return "";
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
            // Загружаем слоты
            slots = SlotsFromTreeAttributes(treeAttribute);

            // Загружаем ContainerSlots
            ContainerSlots = new Dictionary<int, int[]>();
            var containerSlotsTree = treeAttribute["containerSlots"] as TreeAttribute;
            if (containerSlotsTree != null)
            {
                foreach (var key in containerSlotsTree.Keys)
                {
                    if (int.TryParse(key, out int slotId))
                    {
                        var arrayAttr = containerSlotsTree[key] as IntArrayAttribute;
                        if (arrayAttr != null)
                        {
                            ContainerSlots[slotId] = arrayAttr.value;
                        }
                    }
                }
            }

            // Загружаем ContainerTypes
            ContainerTypes = new Dictionary<int, string>();
            var containerTypesTree = treeAttribute["containerTypes"] as TreeAttribute;
            if (containerTypesTree != null)
            {
                foreach (var key in containerTypesTree.Keys)
                {
                    if (int.TryParse(key, out int slotId))
                    {
                        var stringAttr = containerTypesTree[key] as StringAttribute;
                        if (stringAttr != null)
                        {
                            ContainerTypes[slotId] = stringAttr.value;
                        }
                    }
                }
            }

            // Загружаем DoubleChestIndex
            DoubleChestIndex = new List<int>();
            var doubleChestTree = treeAttribute["doubleChestIndex"] as IntArrayAttribute;
            if (doubleChestTree != null)
            {
                DoubleChestIndex.AddRange(doubleChestTree.value);
            }
        }

        /// <summary>
        /// Находит ID слота контейнера для данного слота предмета, учитывая двойные сундуки
        /// </summary>
        /// <param name="slotId">ID слота предмета</param>
        /// <returns>ID слота контейнера или -1 если не найден</returns>
        public int GetContainerSlotIdForSlot(int slotId)
        {
            // Ищем в ContainerSlots по ключам (которые теперь равны слотам контейнеров)
            foreach (var kvp in ContainerSlots)
            {
                if (kvp.Value.Contains(slotId))
                {
                    return kvp.Key; // Теперь это реальный слот контейнера
                }
            }

            return -1;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
            // Сохраняем ContainerSlots
            var containerSlotsTree = new TreeAttribute();
            foreach (var kvp in ContainerSlots)
            {
                var slotArray = new IntArrayAttribute(kvp.Value);
                containerSlotsTree[kvp.Key.ToString()] = slotArray;
            }
            tree["containerSlots"] = containerSlotsTree;

            // Сохраняем ContainerTypes
            var containerTypesTree = new TreeAttribute();
            foreach (var kvp in ContainerTypes)
            {
                var stringAttr = new StringAttribute(kvp.Value);
                containerTypesTree[kvp.Key.ToString()] = stringAttr;
            }
            tree["containerTypes"] = containerTypesTree;

            // Сохраняем DoubleChestIndex
            tree["doubleChestIndex"] = new IntArrayAttribute(DoubleChestIndex?.ToArray() ?? new int[0]);

        }
    }
}
