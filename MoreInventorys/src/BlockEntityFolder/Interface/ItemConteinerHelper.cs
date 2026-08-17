using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoreInventorys.src.BlockEntityFolder.Interface
{
    public static class ItemConteinerHelper
    {
        public static bool IsValidContainer(string path)
        {
            return false;
            /*return !string.IsNullOrEmpty(path) &&
            ModConfigFile.Current?.VanilaStorageItemContainersCode?.Keys
               .Any(key => path.StartsWith(key)) == true;*/
        }

        public static int GetQuantitySlots(string path)
        {
            int value = 0;
            if (ModConfigFile.Current.VanilaStorageItemContainersCode.TryGetValue(path, out value)) return value;
            else return 0;
        }
    }
}
