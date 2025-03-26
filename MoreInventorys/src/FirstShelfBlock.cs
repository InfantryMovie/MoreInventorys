using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src
{
  public class FirstShelfBlock : Block
  {
        public override void OnLoaded(ICoreAPI api)
        {

            base.OnLoaded(api);
            // Todo: Add interaction help

        }
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
          BEFirstShelf beshelf = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFirstShelf;

            if (beshelf != null) return beshelf.OnInteract(byPlayer, blockSel);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
   }   

}
