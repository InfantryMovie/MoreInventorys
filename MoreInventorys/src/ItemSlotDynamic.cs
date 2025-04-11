using MoreInventorys.src.InventoryFolder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoreInventorys.src
{
    public class ItemSlotDynamic : ItemSlotSurvival
    {
        
        public int SlotId { get; }
        int MaxContainerBlockSlots;
        public ItemSlotDynamic(InventoryBase inventory, int slotId) : base(inventory)
        {
            this.SlotId = slotId;
            
            if(inventory is InventoryDynamic inv)
            {
                MaxContainerBlockSlots = inv.MaxContainerBlockSlots;

                switch (inv.MaxContainerBlockSlots)
                {
                    case 3:
                        if (this.SlotId == 0 || this.SlotId == 1 || this.SlotId == 2) MaxSlotStackSize = 1;
                        break;
                    case 6:
                        if (this.SlotId == 0 || this.SlotId == 1 || this.SlotId == 2 ||
                            this.SlotId == 3 || this.SlotId == 4 || this.SlotId == 5) MaxSlotStackSize = 1;
                        break;

                    default:
                        break;
                }
            }

        }

        public (bool, int quantitySlots, Block container) IsContainer(ItemSlot slot)
        {
            
            var storageBlock = slot.Itemstack.Block;
            var defaultType = storageBlock?.Attributes?["defaultType"]?.ToString();
            var quantitySlots = storageBlock?.Attributes?["quantitySlots"]?[defaultType]?.AsInt();

            if (quantitySlots == null || quantitySlots == 0) return (false, 0, null);

            return (true, (int)quantitySlots, storageBlock);
        }

        public override bool CanTake()
        {
            switch (MaxContainerBlockSlots)
            {
                case 3:
                    if (this.SlotId == 0 || this.SlotId == 1 || this.SlotId == 2) return false;
                    break;
                case 6:
                    if (this.SlotId == 0 || this.SlotId == 1 || this.SlotId == 2 ||
                        this.SlotId == 3 || this.SlotId == 4 || this.SlotId == 5) return false;
                    break;

                default:
                    return false;
                   // break;
            }

            //-----------логика для удаления контейнера из стеллажа:
            // текущие проблемы:
            // - после удаления контейнера нужно перерисовать интерфейс (слотов должно стать меньше)
            // - после кода ниже, удаляются слоты и получаем кучу ошибок, пока не разобрался

            /*bool result =  CanTakeContainer(SlotId);
                 if(result)
                 {
                     //контейнер пуст, удаляем из списка, отдаем контейнер

                     if (inventory is InventoryDynamic inv)
                     {
                         if (inv.ContainerSlots == null) return false;

                         //список SlotId которые нужно удалить, после того как убрали контейнер со стеллажа
                         List<int> slotsToRemove = new List<int>();
                         lock (inv.LockContainerSlots)
                         {
                             if (inv.ContainerSlots.ContainsKey(SlotId))
                             {
                                 slotsToRemove = inv.ContainerSlots[SlotId].ToList();

                                 //из за удаление слотов все крашится, пока оставим, убираем возможность забрать контейнер, только ломать стеллаж
                                 bool removeResult = inv.RemoveSlots(slotsToRemove.ToArray());
                                 inv.ContainerSlots.Remove(SlotId);

                             }
                         }

                         inv.containerBlockSlotsActive--;
                         inv.dynamicSlots -= slotsToRemove.Count;
                     }
                 }*/

            return base.CanTake();
        }
        public override bool CanHold(ItemSlot sourceSlot)
        {
            if(!sourceSlot.Empty)
            {
                var containerResult = IsContainer(sourceSlot);
                bool isContainer = containerResult.Item1;
                int quantitySlots = containerResult.quantitySlots;
                var container = containerResult.container;

                switch (MaxContainerBlockSlots)
                {
                    case 3:
                        if (this.SlotId == 0 || this.SlotId == 1 || this.SlotId == 2) return false;
                        break;
                    case 6:
                        if (this.SlotId == 0 || this.SlotId == 1 || this.SlotId == 2 ||
                            this.SlotId == 3 || this.SlotId == 4 || this.SlotId == 5) return false;
                        break;

                    default:
                        return false;
                }

                //---------логика для возможности установить контейнер для получения слотов прямо из интерфейса:
                // текущая проблема - как перерисовать интерфейс после этого?

                /*if (isContainer && container != null)
                    {
                        AddContainer(SlotId, quantitySlots);
                        return true;
                    } 

                    else return false;*/

            }

            return base.CanHold(sourceSlot);
        }





        bool CanTakeContainer(int containerSlotId)
        {
            if (inventory is InventoryDynamic inv)
            {
                if(inv.ContainerSlots == null || inv.ContainerSlots.Count == 0) return false;

                lock (inv.LockContainerSlots)
                {
                    int[] slotsId = inv.ContainerSlots[containerSlotId];
                    var filteredInv = new List<ItemSlotDynamic>();

                    for (int i = 0; i < inv.Count; i++)
                    {
                        if (slotsId.Contains(i))
                        {
                            filteredInv.Add(inv[i]);
                        }
                    }

                    if (filteredInv.All(slot => slot.Empty)) return true;
                    
                }
            }

            return false;
        }
        void AddContainer(int slotId, int quantitySlots)
        {
            if (inventory is InventoryDynamic inv)
            {
                if (inv.ContainerSlots == null) return;

                //записываем сколько и какие конкретно дал слоты данный контейнер, нужно для логики дать/забрать контейнер со стеллажа
                int lastId = inv[inv.Count - 1].SlotId;
                int[] quantitySlotsId = Enumerable.Range(lastId+1, quantitySlots).ToArray();

                lock (inv.LockContainerSlots)
                {
                    inv.ContainerSlots.Add(slotId, quantitySlotsId);
                }

                //нужно добавить слотов на стеллаж от текущего контейнера который мы только что поставили на стеллаж через интерфейс
                //увеличиваем слоты стеллажа
                inv.AddSlots(quantitySlots);
                inv.dynamicSlots += quantitySlots;
                inv.containerBlockSlotsActive++;

                //тут надо как то закрыть и открыть интерфейс или перерисовать,
                //в открытом интерфейсе мы положили контейнер который дал слоты но мы это не видимо пока не закроем и не откроем интерфейс заного
            }
        }
    }

}
