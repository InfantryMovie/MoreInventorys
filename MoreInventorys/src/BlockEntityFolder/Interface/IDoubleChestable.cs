using System;
using System.Collections.Generic;
using System.Text;

namespace MoreInventorys.src.BlockEntityFolder.Interface
{
    internal interface IDoubleChestable : IRackable
    {
        int MaxDoubleChests => Columns < 2
                ? 0
                : (Columns / 2) * Rows;

        protected List<int> DoubleChestIndexs { get; set; }
        int doubleChestIndex1 { get; set; }


        /// <summary>
        /// Вызывать в конструкторе наследника
        /// </summary>
        void InitializeDoubleChestIndexs()
        {
            for (int col = 0; col < Columns; col++)
            {
                DoubleChestIndexs.Add(col);
            }
        }

        

    }
}
