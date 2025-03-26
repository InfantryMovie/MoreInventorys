using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;



namespace MoreInventorys.src
{
    // Основной класс мода
    public class MoreInventorysMod : ModSystem
    {
        // Метод, вызываемый при загрузке мода
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            // Регистрируем наш блок "полка"
            api.RegisterBlockClass("firstshelfblock", typeof(FirstShelfBlock));
            api.RegisterBlockEntityClass("beshelf", typeof(BEFirstShelf));

            // Выводим сообщение в консоль, чтобы убедиться, что мод загружен
            api.Logger.Notification("Mod 'More Inventorys' успешно загружен!");
        }

       /* public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }*/
    }
}
