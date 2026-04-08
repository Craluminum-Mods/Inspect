using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Inspect.HarmonyPatches;

[HarmonyPatch(typeof(SlideshowGridRecipeTextComponent), nameof(SlideshowGridRecipeTextComponent.RenderInteractiveElements))]
public static class SlideshowGridRecipeTextComponent_Patch
{
    [HarmonyPostfix]
    public static void Postfix(SlideshowGridRecipeTextComponent __instance, float deltaTime, double renderX, double renderY, double renderZ, ICoreClientAPI ___capi, int ___currentItemIndex, double ___size, int[][,] ___variantDisplaySequence, int ___secondCounter)
    {
        if (GuiDialogInspect.LockStack) return;

        LineRectangled bounds = __instance.BoundsPerLine[0];

        GridRecipeAndUnnamedIngredients recipeunin = __instance.GridRecipesAndUnnamedIngredients[___currentItemIndex];

        double rx = 0, ry = 0;
        int mx = ___capi.Input.MouseX;
        int my = ___capi.Input.MouseY;

        DummySlot dummySlot = new DummySlot();

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int index = recipeunin.Recipe.GetGridIndex(y, x, recipeunin.Recipe.ResolvedIngredients, recipeunin.Recipe.Width);

                rx = renderX + bounds.X + x * (___size + GuiElement.scaled(3));
                ry = renderY + bounds.Y + y * (___size + GuiElement.scaled(3));

                CraftingRecipeIngredient? ingred = recipeunin.Recipe.GetElementInGrid(y, x, recipeunin.Recipe.ResolvedIngredients, recipeunin.Recipe.Width);
                if (ingred == null)
                {
                    continue;
                }

                if (recipeunin.UnnamedIngredients?.TryGetValue(index, out ItemStack[]? unnamedWildcardStacklist) == true && unnamedWildcardStacklist.Length > 0)
                {
                    dummySlot.Itemstack = unnamedWildcardStacklist[___variantDisplaySequence[___secondCounter % 30][x, y] % unnamedWildcardStacklist.Length]?.Clone();
                }
                else
                {
                    dummySlot.Itemstack = ingred.ResolvedItemStack?.Clone();
                }
                
                double dx = mx - rx + 1;
                double dy = my - ry + 2;

                if (dx >= 0 && dx < ___size && dy >= 0 && dy < ___size)
                {
                    GuiDialogInspect.SetStack(dummySlot?.Itemstack?.Clone());
                }
            }
        }
    }
}