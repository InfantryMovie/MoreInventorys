using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
namespace MoreInventorys.src.BlockEntityFolder
{
    public class BEFirstShelf : BlockEntityDisplay
    {
        protected static int slotCount = 8;

        protected InventoryGeneric inv;

        public override InventoryBase Inventory => (InventoryBase)(object)inv;

        public override string InventoryClassName => "firstshelfinventory";

        public override string AttributeTransformCode => "onshelfTransform";
        protected string GetSlotType(int slotid) => "firstshelfinventory";

        /*public float meshAngle;

        private MeshData currentMesh;

        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "firstshelfinventory";

        public override string AttributeTransformCode => "onshelfTransform";

        Block block;

        static int slotCount = 8;*/

        public BEFirstShelf()
        {
            inv = new InventoryGeneric(slotCount, "firstshelfinventory-0", null, (id, inv) => new ItemSlotDisplay(inv, GetSlotType(id)));
            //inv = new InventoryGeneric(slotCount, "firstshelf-0", null);
            //inv.AddSlots(slotCount);
        }



        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            // Must be added after Initialize(), so we can override the transition speed value
            inv.OnAcquireTransitionSpeed += Inv_OnAcquireTransitionSpeed;
        }
        /*public override void Initialize(ICoreAPI api)
        {
            block = api.World.BlockAccessor.GetBlock(Pos);
            base.Initialize(api);

        }*/


        /* 1.22 - нет метода =/
         * protected override float Inventory_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            if (transType == EnumTransitionType.Dry || transType == EnumTransitionType.Melt)
            {
                Room obj = room;
                if (obj == null || obj.ExitCount != 0)
                {
                    return 0.5f;
                }
                return 2f;
            }
            if (Api == null)
            {
                return 0f;
            }
            if (transType == EnumTransitionType.Perish || transType == EnumTransitionType.Ripen)
            {
                float perishRate = GetPerishRate();
                if (transType == EnumTransitionType.Ripen)
                {
                    return GameMath.Clamp((1f - perishRate - 0.5f) * 3f, 0f, 1f);
                }
                return baseMul * perishRate;
            }
            return 1f;
        }*/

        protected float Inv_OnAcquireTransitionSpeed(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            if (transType is EnumTransitionType.Dry or EnumTransitionType.Melt)
            {
                // Since we can now have multiple OnAcquireTransitionSpeed invocations stacked we have to multiply this to offset the base 0.25f
                return (container.Room?.ExitCount == 0 ? 2f : 0.5f) * 4f;
            }
            if (Api == null) return 0;

            if (transType is not EnumTransitionType.Ripen) return 1;

            return GameMath.Clamp((1 - container.GetPerishRate() - 0.5f) * 3, 0, 1);
        }

        public bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (TryUse(byPlayer, blockSel)) return true;
            else if (slot.Empty) return TryTake(byPlayer, blockSel);
            else if (GetShelvableLayout(slot.Itemstack) != null) return TryPut(byPlayer, blockSel);

            return false;
        }


        public bool CanUse(ItemStack? stack, BlockSelection blockSel)
        {
            if (stack == null) return false;

            var obj = stack.Collectible;
            bool up = blockSel.SelectionBoxIndex > 1;
            bool left = (blockSel.SelectionBoxIndex % 2) == 0;
            var shelvableLayout = GetShelvableLayout(inv[up ? 4 : 0].Itemstack);
            if (shelvableLayout is not EnumShelvableLayout.SingleCenter)
            {
                if (!left) shelvableLayout = GetShelvableLayout(inv[up ? 6 : 2].Itemstack);
            }

            int start = (up ? 4 : 0) + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 0 : (left ? 0 : 2));
            int end = start + (shelvableLayout is EnumShelvableLayout.Halves or EnumShelvableLayout.SingleCenter ? 1 : 2);

            CollectibleObject invColl;
            for (int i = end - 1; i >= start; i--)
            {
                if (inv[i].Empty) continue;

                invColl = inv[i].Itemstack.Collectible;

                if (obj?.Attributes?["mealContainer"]?.AsBool() == true || obj is IContainedInteractable or IBlockMealContainer)
                {
                    return invColl is BlockCookedContainerBase;
                }

                if (obj?.Attributes?["canSealCrock"]?.AsBool() == true)
                {
                    return invColl is BlockCrock;
                }
            }

            return false;
        }

        /* internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
         {

             ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
             if (slot.Empty)
             {
                 if (TryTake(byPlayer, blockSel))
                 {
                     return true;
                 }
                 return false;
             }
             CollectibleObject colObj = slot.Itemstack.Collectible;
             if (colObj.Attributes != null && colObj.Attributes["shelvable"].AsBool())
             {
                 AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
                 if (TryPut(slot, blockSel))
                 {
                     Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                     MarkDirty();
                     return true;
                 }
                 return false;
             }

             return false;
         }*/
        public bool CanPlace(ItemStack? stack, BlockSelection blockSel, out bool canTake)
        {
            bool up = blockSel.SelectionBoxIndex > 1;
            bool left = (blockSel.SelectionBoxIndex % 2) == 0;

            if (GetShelvableLayout(inv[up ? 4 : 0].Itemstack) is EnumShelvableLayout shelvableLayoutFullSlot &&
                (shelvableLayoutFullSlot is EnumShelvableLayout.SingleCenter || (shelvableLayoutFullSlot is EnumShelvableLayout.Halves && left)) ||
                (GetShelvableLayout(inv[up ? 6 : 2].Itemstack) is EnumShelvableLayout.Halves && !left))
            {
                canTake = true;
                return false;
            }

            var shelvableLayout = GetShelvableLayout(stack);

            int start = (up ? 4 : 0) + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 0 : (left ? 0 : 2));
            int end = start + (shelvableLayout is EnumShelvableLayout.Halves or EnumShelvableLayout.SingleCenter ? 1 : 2);

            canTake = false;
            bool canPlace = false;
            for (int i = end - 1; i >= start; i--)
            {
                if (inv[i].Empty) canPlace = true;
                else canTake = true;
            }

            return canPlace;
        }

        private bool TryUse(IPlayer player, BlockSelection blockSel)
        {
            bool up = blockSel.SelectionBoxIndex > 1;
            bool left = (blockSel.SelectionBoxIndex % 2) == 0;
            var shelvableLayout = GetShelvableLayout(inv[up ? 4 : 0].Itemstack);
            if (shelvableLayout is not EnumShelvableLayout.SingleCenter)
            {
                if (!left) shelvableLayout = GetShelvableLayout(inv[up ? 6 : 2].Itemstack);
            }

            int start = (up ? 4 : 0) + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 0 : (left ? 0 : 2));
            int end = start + (shelvableLayout is EnumShelvableLayout.Halves or EnumShelvableLayout.SingleCenter ? 1 : 2);

            if (player.Entity.Controls.ShiftKey) return false;

            for (int i = end - 1; i >= start; i--)
            {
                var collIci = inv[i].Itemstack?.Collectible.GetCollectibleInterface<IContainedInteractable>();
                if (collIci != null)
                {
                    if (collIci.OnContainedInteractStart(this, inv[i], player, blockSel))
                    {
                        MarkDirty();
                        return true;
                    }
                }
            }

            return false;
        }
        private bool TryPut(IPlayer byPlayer, BlockSelection blockSel)
        {
            var heldSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            bool up = blockSel.SelectionBoxIndex > 1;
            bool left = (blockSel.SelectionBoxIndex % 2) == 0;

            int filledSlots = 0;
            var shelvableLayout = GetShelvableLayout(heldSlot.Itemstack);

            int start = (up ? 4 : 0) + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 0 : (left ? 0 : 2));
            int end = start + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 4 : 2);

            if (shelvableLayout is EnumShelvableLayout.Halves or EnumShelvableLayout.SingleCenter)
            {
                for (int i = start; i < end; i++)
                {
                    if (!inv[i].Empty)
                    {
                        var layout = GetShelvableLayout(inv[i].Itemstack);
                        filledSlots += layout is EnumShelvableLayout.SingleCenter ? 4 : layout is EnumShelvableLayout.Halves ? 2 : 1;
                    }
                }
            }

            if (filledSlots > 0 && filledSlots < (shelvableLayout is EnumShelvableLayout.SingleCenter ? 4 : 2))
            {
                (Api as ICoreClientAPI)?.TriggerIngameError(this, "needsmorespace", Lang.Get("shelfhelp-needsmorespace-error"));
                return false;
            }

            if (shelvableLayout is not EnumShelvableLayout.SingleCenter) shelvableLayout = GetShelvableLayout(inv[up ? 4 : 0].Itemstack);
            if (shelvableLayout is not EnumShelvableLayout.SingleCenter && !left) shelvableLayout = GetShelvableLayout(inv[up ? 6 : 2].Itemstack);

            start = (up ? 4 : 0) + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 0 : (left ? 0 : 2));
            end = start + (shelvableLayout is EnumShelvableLayout.Halves or EnumShelvableLayout.SingleCenter ? 1 : 2);

            for (int i = start; i < end; i++)
            {
                if (!inv[i].Empty) continue;

                int moved = heldSlot.TryPutInto(Api.World, inv[i]);
                MarkDirty();
                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                if (moved > 0)
                {
                    Api.World.PlaySoundAt(inv[i].Itemstack?.Block?.Sounds?.Place ?? GlobalConstants.DefaultBuildSound, byPlayer.Entity, byPlayer);
                    Api.World.Logger.Audit("{0} Put 1x{1} into Shelf at {2}.",
                        byPlayer.PlayerName,
                        inv[i].Itemstack?.Collectible.Code,
                        Pos
                    );
                    return true;
                }

                return false;
            }

            (Api as ICoreClientAPI)?.TriggerIngameError(this, "shelffull", Lang.Get("shelfhelp-shelffull-error"));
            return false;
        }


        /*private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            bool num = blockSel.SelectionBoxIndex > 1;
            bool left = blockSel.SelectionBoxIndex % 2 == 0;
            int num2 = (num ? 4 : 0) + (!left ? 2 : 0);
            int end = num2 + 2;
            for (int i = num2; i < end; i++)
            {
                if (inv[i].Empty)
                {
                    int num3 = slot.TryPutInto(Api.World, inv[i]);
                    MarkDirty();
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return num3 > 0;
                }
            }
            return false;
        }*/

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            bool up = blockSel.SelectionBoxIndex > 1;
            bool left = (blockSel.SelectionBoxIndex % 2) == 0;
            var shelvableLayout = GetShelvableLayout(inv[up ? 4 : 0].Itemstack);
            if (shelvableLayout is not EnumShelvableLayout.SingleCenter)
            {
                if (!left) shelvableLayout = GetShelvableLayout(inv[up ? 6 : 2].Itemstack);
            }

            int start = (up ? 4 : 0) + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 0 : (left ? 0 : 2));
            int end = start + (shelvableLayout is EnumShelvableLayout.SingleCenter ? 4 : 2);

            for (int i = end - 1; i >= start; i--)
            {
                if (inv[i].Empty) continue;

                ItemStack? stack = inv[i].TakeOut(1);
                if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    SoundAttributes? sound = stack?.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound ?? GlobalConstants.DefaultBuildSound, byPlayer.Entity, byPlayer);
                }

                if (stack?.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos);
                }
                Api.World.Logger.Audit("{0} Took 1x{1} from Shelf at {2}.",
                    byPlayer.PlayerName,
                    stack?.Collectible.Code,
                    Pos
                );

                (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                MarkDirty();

                return true;
            }

            return false;
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];

            for (int index = 0; index < slotCount; index++)
            {
                var shelvableType = GetShelvableLayout(inv[index].Itemstack);

                float x = ((index % 4) >= 2) ? 12 / 16f : 4 / 16f;
                float y = index >= 4 ? 10 / 16f : 2 / 16f;
                float z = (index % 2 == 0) ? 4 / 16f : 10 / 16f;

                if (index is 0 or 4 && shelvableType is EnumShelvableLayout.SingleCenter) x = 0.5f;
                if (index is 0 or 2 or 4 or 6 && shelvableType is EnumShelvableLayout.Halves or EnumShelvableLayout.SingleCenter) z = 0.4f;

                tfMatrices[index] =
                    new Matrixf()
                    .Translate(0.5f, 0, 0.5f)
                    .RotateYDeg(Block.Shape.rotateY)
                    .Translate(x - 0.5f, y, z - 0.5f)
                    .Translate(-0.5f, 0, -0.5f)
                    .Values
                ;
            }

            return tfMatrices;
        }


        public static EnumShelvableLayout? GetShelvableLayout(ItemStack? stack)
        {
            if (stack == null)
            {
                return null;
            }
            JsonObject attr = stack.Collectible?.Attributes;
            CollectibleObject collectible = stack.Collectible;
            EnumShelvableLayout? layout = ((collectible == null) ? null : collectible.GetCollectibleInterface<IShelvable>()?.GetShelvableType(stack));
            EnumShelvableLayout? enumShelvableLayout = layout;
            if (!enumShelvableLayout.HasValue)
            {
                layout = ((attr != null) ? attr["shelvable"].AsString((string)null) : null) switch
                {
                    "Quadrants" => EnumShelvableLayout.Quadrants,
                    "Halves" => EnumShelvableLayout.Halves,
                    "SingleCenter" => EnumShelvableLayout.SingleCenter,
                    _ => null,
                };
            }
            enumShelvableLayout = layout;
            if (!enumShelvableLayout.HasValue)
            {
                layout = ((attr != null && attr["shelvable"].AsBool(false)) ? new EnumShelvableLayout?(EnumShelvableLayout.Quadrants) : null);
            }
            return layout;
        }

        /*private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            bool num = blockSel.SelectionBoxIndex > 1;
            bool left = blockSel.SelectionBoxIndex % 2 == 0;
            int start = (num ? 4 : 0) + (!left ? 2 : 0);
            for (int i = start + 2 - 1; i >= start; i--)
            {
                if (!inv[i].Empty)
                {
                    ItemStack stack = inv[i].TakeOut(1);
                    if (byPlayer.InventoryManager.TryGiveItemstack(stack))
                    {
                        AssetLocation sound = stack.Block?.Sounds?.Place;
                        Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                    }
                    if (stack.StackSize > 0)
                    {
                        Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                    (Api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    MarkDirty();
                    return true;
                }
            }
            return false;
        }*/

        /*protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];
            for (int index = 0; index < slotCount; index++)
            {
                float x = index % 4 >= 2 ? 0.75f : 0.25f;
                float y = index >= 4 ? 0.560f : 0.06f;
                float z = index % 2 == 0 ? 0.25f : 0.625f;
                tfMatrices[index] = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(block.Shape.rotateY).Translate(x - 0.5f, y, z - 0.4f)
                    .Translate(-0.5f, 0f, -0.5f)
                    .Values;
            }
            return tfMatrices;
        }*/


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            // Do this last!!!
            RedrawAfterReceivingTreeAttributes(worldForResolving);     // Redraw on client after we have completed receiving the update from server
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);


            float ripenRate = GameMath.Clamp(((1 - container.GetPerishRate()) - 0.5f) * 3, 0, 1);
            if (ripenRate > 0)
            {
                sb.Append(Lang.Get("Suitable spot for food ripening."));
            }

            sb.AppendLine();

            bool up = forPlayer.CurrentBlockSelection != null && forPlayer.CurrentBlockSelection.SelectionBoxIndex > 1;

            for (int j = 3; j >= 0; j--)
            {
                int i = j + (up ? 4 : 0);
                i ^= 2;   //Display shelf contents text for items from left-to-right, not right-to-left

                if (inv[i].Empty) continue;

                ItemStack? stack = inv[i].Itemstack;

                var transitionableProps = stack?.Collectible?.GetTransitionableProperties(Api.World, stack, forPlayer.Entity);
                if (transitionableProps != null && transitionableProps.Length > 0)
                {
                    sb.Append(PerishableInfoCompact(Api, inv[i], ripenRate));
                }
                else
                {
                    sb.AppendLine(stack?.Collectible.GetCollectibleInterface<IContainedCustomName>()?.GetContainedInfo(inv[i]) ?? stack?.GetName() ?? Lang.Get("unknown"));
                }
            }
        }

        public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
        {
            if (contentSlot.Empty) return "";

            StringBuilder dsc = new StringBuilder();

            if (withStackName)
            {
                dsc.Append(contentSlot.Itemstack.GetName());
            }

            TransitionState[]? transitionStates = contentSlot.Itemstack.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
            if (transitionStates == null) return dsc.ToString();

            bool nowSpoiling = false;
            bool appendLine = false;
            for (int i = 0; i < transitionStates.Length; i++)
            {
                TransitionState state = transitionStates[i];

                TransitionableProperties prop = state.Props;
                float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, prop.Type);

                if (perishRate <= 0) continue;

                float transitionLevel = state.TransitionLevel;
                float freshHoursLeft = state.FreshHoursLeft / perishRate;

                switch (prop.Type)
                {
                    case EnumTransitionType.Perish:

                        appendLine = true;

                        if (transitionLevel > 0)
                        {
                            nowSpoiling = true;
                            dsc.Append(", " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100)));
                        }
                        else
                        {
                            double hoursPerday = Api.World.Calendar.HoursPerDay;

                            if (freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
                            {
                                dsc.Append(", " + Lang.Get("fresh for {0} years", Math.Round(freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
                            }
                            else if (freshHoursLeft > hoursPerday)
                            {
                                dsc.Append(", " + Lang.Get("fresh for {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                            }
                            else
                            {
                                dsc.Append(", " + Lang.Get("fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
                            }
                        }
                        break;

                    case EnumTransitionType.Ripen:
                        if (nowSpoiling) break;

                        appendLine = true;

                        if (transitionLevel > 0)
                        {
                            dsc.Append(", " + Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100), (state.TransitionHours - state.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
                        }
                        else
                        {
                            double hoursPerday = Api.World.Calendar.HoursPerDay;

                            if (freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
                            {
                                dsc.Append(", " + Lang.Get("will ripen in {0} years", Math.Round(freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
                            }
                            else if (freshHoursLeft > hoursPerday)
                            {
                                dsc.Append(", " + Lang.Get("will ripen in {0} days", Math.Round(freshHoursLeft / hoursPerday, 1)));
                            }
                            else
                            {
                                dsc.Append(", " + Lang.Get("will ripen in {0} hours", Math.Round(freshHoursLeft, 1)));
                            }
                        }
                        break;
                }
            }

            if (appendLine) dsc.AppendLine();

            return dsc.ToString();
        }

       
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            //tree.SetFloat("meshAngle", meshAngle);
        }

        
        

        

        /* в 122 нет GetPerishRate
         * public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);
            float ripenRate = GameMath.Clamp((1f - GetPerishRate() - 0.5f) * 3f, 0f, 1f);
            if (ripenRate > 0f)
            {
                sb.Append(Lang.Get("Suitable spot for food ripening."));
            }
            sb.AppendLine();
            bool up = forPlayer.CurrentBlockSelection != null && forPlayer.CurrentBlockSelection.SelectionBoxIndex > 1;
            for (int j = 3; j >= 0; j--)
            {
                int i = j + (up ? 4 : 0);
                i ^= 2;
                if (!inv[i].Empty)
                {
                    ItemStack stack = inv[i].Itemstack;
                    if (stack.Collectible is BlockCrock)
                    {
                        sb.Append(CrockInfoCompact(inv[i]));
                    }
                    else if (stack.Collectible.TransitionableProps != null && stack.Collectible.TransitionableProps.Length != 0)
                    {
                        sb.Append(PerishableInfoCompact(Api, inv[i], ripenRate));
                    }
                    else
                    {
                        sb.AppendLine(stack.GetName());
                    }
                }
            }
            //тут можно добавить описание к блоку которое будет видно при наведении на блок в мире
            //sb.AppendLine();
            //sb.AppendLine("Че тут будет?\nПроверка 1\nПроверка 2");

        }*/

       

        /*public string CrockInfoCompact(ItemSlot inSlot)
        {
            Api.World.GetBlock(new AssetLocation("bowl-meal"));
            BlockCrock crock = inSlot.Itemstack.Collectible as BlockCrock;
            IWorldAccessor world = Api.World;
            CookingRecipe recipe = crock.GetCookingRecipe(world, inSlot.Itemstack);
            ItemStack[] stacks = crock.GetNonEmptyContents(world, inSlot.Itemstack);
            if (stacks == null || stacks.Length == 0)
            {
                return Lang.Get("Empty Crock") + "\n";
            }
            StringBuilder dsc = new StringBuilder();
            if (recipe != null)
            {
                double servings = inSlot.Itemstack.Attributes.GetDecimal("quantityServings");
                if (recipe != null)
                {
                    if (servings == 1.0)
                    {
                        dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
                    }
                    else
                    {
                        dsc.Append(Lang.Get("{0:0.#}x {1}.", servings, recipe.GetOutputName(world, stacks)));
                    }
                }
            }
            else
            {
                int i = 0;
                ItemStack[] array = stacks;
                foreach (ItemStack stack2 in array)
                {
                    if (stack2 != null)
                    {
                        if (i++ > 0)
                        {
                            dsc.Append(", ");
                        }
                        dsc.Append(stack2.StackSize + "x " + stack2.GetName());
                    }
                }
                dsc.Append(".");
            }
            DummyInventory dummyInv = new DummyInventory(Api);
            ItemSlot contentSlot = BlockCrock.GetDummySlotForFirstPerishableStack(Api.World, stacks, null, dummyInv);
            dummyInv.OnAcquireTransitionSpeed = (transType, stack, mul) => mul * crock.GetContainingTransitionModifierContained(world, inSlot, transType) * inv.GetTransitionSpeedMul(transType, stack);
            TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
            bool addNewLine = true;
            if (transitionStates != null)
            {
                foreach (TransitionState state in transitionStates)
                {
                    TransitionableProperties prop = state.Props;
                    float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(world, contentSlot, prop.Type);
                    if (perishRate <= 0f)
                    {
                        continue;
                    }
                    addNewLine = false;
                    float transitionLevel = state.TransitionLevel;
                    float freshHoursLeft = state.FreshHoursLeft / perishRate;
                    if (prop.Type != 0)
                    {
                        continue;
                    }
                    if (transitionLevel > 0f)
                    {
                        dsc.AppendLine(" " + Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100f)));
                        continue;
                    }
                    double hoursPerday = Api.World.Calendar.HoursPerDay;
                    if ((double)freshHoursLeft / hoursPerday >= Api.World.Calendar.DaysPerYear)
                    {
                        dsc.AppendLine(" " + Lang.Get("Fresh for {0} years", Math.Round((double)freshHoursLeft / hoursPerday / Api.World.Calendar.DaysPerYear, 1)));
                    }
                    else if ((double)freshHoursLeft > hoursPerday)
                    {
                        dsc.AppendLine(" " + Lang.Get("Fresh for {0} days", Math.Round((double)freshHoursLeft / hoursPerday, 1)));
                    }
                    else
                    {
                        dsc.AppendLine(" " + Lang.Get("Fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
                    }
                }
            }
            if (addNewLine)
            {
                dsc.AppendLine("");
            }
            return dsc.ToString();
        }*/

        /*public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            //IL_001b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0035: Expected O, but got Unknown
            mesher.AddMeshData(currentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, meshAngle, 0f), 1);
            return true;
        }*/
    }
}