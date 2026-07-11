using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockFolder
{
    public class CrateClosedBlock : Block
    {
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);
            // Todo: Add interaction help

        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            /* dsc.AppendLine();
             dsc.AppendLine("Колличество слотов: 16");
             dsc.AppendLine();
             dsc.AppendLine(Lang.Get("moreinventorys:block-micrateclosed-north-desc"));*/

            // Проверяем, есть ли у блока атрибут capacity
            if (Attributes != null)
            {
                if (Attributes["capacity"] == null) return;

                int capacity = Attributes["capacity"].AsInt(0);
                if (capacity > 0)
                {
                    // Добавляем строчку ПЕРЕД описанием (в верхнюю часть)
                    dsc.AppendLine(Lang.Get("Слотов для хранения: {0}", capacity));
                }
            }

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            BECrateClosed be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECrateClosed;

            if (be != null) return be.OnInteract(byPlayer, blockSel);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

}
