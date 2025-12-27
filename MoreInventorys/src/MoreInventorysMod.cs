using System.Collections.Generic;
using MoreInventorys.src.BlockFolder;
using MoreInventorys.src.BlockEntityFolder;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Foundation.Extensions;




namespace MoreInventorys.src
{
    public class ModConfigFile
    {
        public static ModConfigFile Current { get; set; }

        //список ванильных поддерживаемых  контейнеров
        public List<string> VanilaStorageContainersCode { get; set; } = new List<string>();

        public List<string> VanilaStorageWeaponsCode { get; set; } = new List<string>();

        public Dictionary<string, int> ModedStorageWeaponsCode { get; set; } = new Dictionary<string, int>();

        //словарь модовых поддерживаемых контейнеров ключ: code/ значение: slot.count)
        public Dictionary<string, int> ModedStorageContainersCode { get; set; } = new Dictionary<string, int>();

    }
    public class MoreInventorysMod : ModSystem
    {
        private ICoreServerAPI serverApi;
        private ICoreClientAPI clientApi;

        public static IServerNetworkChannel serverChannel;

        public static IClientNetworkChannel clientChannel;


        public override void StartPre(ICoreAPI api)
        {
            ModConfigFile.Current = api.LoadOrCreateConfig<ModConfigFile>("MoreInventorysConfig.json");
           

        }

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

            api.RegisterBlockClass("dummyrh", typeof(DummyRH));
            api.RegisterBlockEntityClass("bedummyrh", typeof(BlockEntityDummy));

            api.RegisterBlockClass("dummyrv", typeof(DummyRV));

            api.RegisterBlockClass("smallhorizontleswordstandblock", typeof(SmallHorizontleSwordStandBlock));
            api.RegisterBlockEntityClass("besmallhorizontleswordstand", typeof(BESmallHorizontleSwordStand));

            api.RegisterBlockClass("rackverticalblock", typeof(RackVerticalBlock));
            api.RegisterBlockEntityClass("berackvertical", typeof(BERackVertical));

            api.RegisterBlockClass("rackhorizontalblock", typeof(RackHorizontalBlock));
            api.RegisterBlockEntityClass("berackhorizontal", typeof(BERackHorizontal));

            api.RegisterBlockClass("rackstickblock", typeof(RackStickBlock));
            api.RegisterBlockEntityClass("berackstick", typeof(BERackStick));


            // Выводим сообщение в консоль, чтобы убедиться, что мод загружен
            api.Logger.Notification("<-----------------Mod 'More Inventorys' успешно загружен!----------------->");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;
            clientChannel = api.Network.GetChannel("moreinventorys");
            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            serverApi = api;
            serverChannel = serverApi.Network.GetChannel("moreinventorys");
            
        }

        public override void Dispose()
        {
            base.Dispose();
            serverChannel = null;
            clientChannel = null;
        }
    }
}
