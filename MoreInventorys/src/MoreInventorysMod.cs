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
using Vintagestory.API.Client;
using Vintagestory.API.Server;



namespace MoreInventorys.src
{
    // Основной класс мода
    public class MoreInventorysMod : ModSystem
    {
        private ICoreServerAPI serverApi;
        private ICoreClientAPI clientApi;

        public static IServerNetworkChannel serverChannel;

        public static IClientNetworkChannel clientChannel;

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

            api.RegisterBlockClass("rackverticaloneblock", typeof(RackVerticalOneBlock));
            api.RegisterBlockEntityClass("berackverticalone", typeof(BERackVerticalOne));

            api.RegisterBlockClass("rackhorizontalblock", typeof(RackHorizontalBlock));
            api.RegisterBlockEntityClass("berackhorizontal", typeof(BERackHorizontal));

            // Выводим сообщение в консоль, чтобы убедиться, что мод загружен
            api.Logger.Notification("Mod 'More Inventorys' успешно загружен!");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;
            clientChannel = api.Network.GetChannel("moreinventorys");
            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            //((ModSystem)this).StartServerSide(api);
            serverApi = api;
            serverChannel = serverApi.Network.GetChannel("moreinventorys");
            
        }

        public override void Dispose()
        {
            //((ModSystem)this).Dispose();
            serverChannel = null;
            clientChannel = null;
        }
    }
}
