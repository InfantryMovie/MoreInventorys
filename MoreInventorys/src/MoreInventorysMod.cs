using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreInventorys.src.BlockFolder;
using MoreInventorys.src.BlockEntityFolder;
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

            // Регистрируем все наши блоки

            api.RegisterBlockClass("firstshelfblock", typeof(FirstShelfBlock));
            api.RegisterBlockEntityClass("beshelf", typeof(BEFirstShelf));

            api.RegisterBlockClass("smallverticalweaponstandblock", typeof(SmallVerticalWeaponstandBlock));
            api.RegisterBlockEntityClass("besmallverticalweaponstand", typeof(BESmallVerticalWeaponstand));

            api.RegisterBlockClass("shieldstandblock", typeof(ShieldStandBlock));
            api.RegisterBlockEntityClass("beshieldstand", typeof(BEShieldStand));

            api.RegisterBlockClass("dummy-up", typeof(DummyUP));

            api.RegisterBlockClass("smallhorizontleswordstandblock", typeof(SmallHorizontleSwordStandBlock));
            api.RegisterBlockEntityClass("besmallhorizontleswordstand", typeof(BESmallHorizontleSwordStand));

            // Выводим сообщение в консоль, чтобы убедиться, что мод загружен
            api.Logger.Notification("Mod 'More Inventorys' успешно загружен!");
        }

       /* public override bool ShouldLoad(EnumAppSide forSide)
        {
            return true;
        }*/
    }
}
