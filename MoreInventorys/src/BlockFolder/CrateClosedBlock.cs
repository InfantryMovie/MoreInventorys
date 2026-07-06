using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreInventorys.src.BlockEntityFolder;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            BECrateClosed be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECrateClosed;

            if (be != null) return be.OnInteract(byPlayer, blockSel);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

}
