using MoreInventorys.src.BlockEntityFolder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoreInventorys.src.BlockFolder
{
    public class FirstShelfBlock : Block
    {
        WorldInteraction[]? interactions;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            PlacedPriorityInteract = true;

            interactions = ObjectCacheUtil.GetOrCreate(api, "firstshelfInteractions", () =>
            {
                List<ItemStack> usableItemStacklist = new List<ItemStack>();
                List<ItemStack> shelvableStacklist = new List<ItemStack>();

                foreach (var obj in api.World.Collectibles)
                {
                    if (obj?.Attributes?["mealContainer"]?.AsBool() == true ||
                        obj is IContainedInteractable ||
                        obj is IBlockMealContainer ||
                        obj?.Attributes?["canSealCrock"]?.AsBool() == true)
                    {
                        usableItemStacklist.Add(new ItemStack(obj));
                    }

                    if (BEFirstShelf.GetShelvableLayout(new ItemStack(obj)) != null)
                    {
                        if (obj is BlockPie pieBlock)
                        {
                            var stack = new ItemStack(obj);
                            stack.Attributes.SetInt("pieSize", 4);
                            stack.Attributes.SetString("topCrustType", "square");
                            stack.Attributes.SetInt("bakeLevel", pieBlock.Variant["state"] switch { "raw" => 0, "partbaked" => 1, "perfect" => 2, "charred" => 3, _ => 0 });
                            ItemStack doughStack = new(api.World.GetItem("dough-spelt"), 2);
                            ItemStack fillingStack = new(api.World.GetItem("fruit-redapple"), 2);
                            pieBlock.SetContents(stack, [doughStack, fillingStack, fillingStack, fillingStack, fillingStack, doughStack]);
                            stack.Attributes.SetFloat("quantityServings", 1);
                            shelvableStacklist.Add(stack);
                        }
                        else shelvableStacklist.Add(new ItemStack(obj));
                    }
                }

                var sstacks = shelvableStacklist.ToArray();

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "Положить на полку",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = sstacks,
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            var beshelf = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BEFirstShelf;
                            if (beshelf == null) return null;

                            if (usableItemStacklist.All(stack => !beshelf.CanUse(stack, bs)))
                            {
                                var result = usableItemStacklist.Where(stack => beshelf.CanPlace(stack, bs, out bool canTake)).ToArray();
                                return result.Length > 0 ? result : null;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Положить на полку",
                        HotKeyCode = "shift",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = sstacks,
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            var beshelf = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BEFirstShelf;
                            if (beshelf == null) return null;

                            if (usableItemStacklist.Any(stack => beshelf.CanUse(stack, bs)))
                            {
                                var result = usableItemStacklist.Where(stack => beshelf.CanPlace(stack, bs, out bool canTake)).ToArray();
                                return result.Length > 0 ? result : null;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "Забрать с полки",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true,
                        ShouldApply = (wi, bs, es) =>
                        {
                            var beshelf = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BEFirstShelf;
                            if (beshelf == null) return false;

                            beshelf.CanPlace(null, bs, out bool canTake);
                            return canTake;
                        }
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var worldInteractions = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            // Проверяем права доступа
            var resp = world.Claims.TestAccess(forPlayer, selection.Position, EnumBlockAccessFlags.Use);
            if (resp == EnumWorldAccessResponse.Granted)
            {
                return worldInteractions?.Concat(interactions!).ToArray() ?? interactions!;
            }

            return worldInteractions ?? new WorldInteraction[0];
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendLine(Lang.Get("moreinventorys:block-firstshelf-desc-storage"));
            dsc.AppendLine();
            dsc.AppendLine(Lang.Get("moreinventorys:block-firstshelf-desc"));
        }

        public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }
        /*public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return true;
        }*/
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            BEFirstShelf be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFirstShelf;

            if (be != null) return be.OnInteract(byPlayer, blockSel);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }

}
