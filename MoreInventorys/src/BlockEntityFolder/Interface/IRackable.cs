using System;
using System.Collections.Generic;
using System.Text;

namespace MoreInventorys.src.BlockEntityFolder.Interface
{
    internal interface IRackable
    {
        int Columns { get; }
        int Rows { get; }
        int MaxContainerSlots { get; }

    }
}
