using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MoreInventorys.src.BlockEntityFolder
{
    public class BlockEntityDummy : BlockEntity
    {
        public BlockPos MainBlockPos { get; set; }

        public BlockEntityDummy()
        {
            MainBlockPos = new BlockPos();
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            MainBlockPos = new BlockPos(tree.GetInt("mainX"), tree.GetInt("mainY"), tree.GetInt("mainZ"));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("mainX", MainBlockPos.X);
            tree.SetInt("mainY", MainBlockPos.Y);
            tree.SetInt("mainZ", MainBlockPos.Z);
        }
    }
}
