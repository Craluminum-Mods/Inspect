using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Inspect.HarmonyPatches;

[HarmonyPatch(typeof(MealstackTextComponent), nameof(MealstackTextComponent.RenderInteractiveElements))]
public static class MealstackTextComponent_Patch
{
    [HarmonyPostfix]
    public static void Postfix(MealstackTextComponent __instance, float deltaTime, double renderX, double renderY, double renderZ, ICoreClientAPI ___capi, DummySlot ___dummySlot)
    {
        if (GuiDialogInspect.LockStack) return;

        LineRectangled bounds = __instance.BoundsPerLine[0];
        int relx = (int)(___capi.Input.MouseX - renderX + __instance.renderOffset.X);
        int rely = (int)(___capi.Input.MouseY - renderY + __instance.renderOffset.Y);
 
        if (bounds.PointInside(relx, rely))
        {
            GuiDialogInspect.SetStack(___dummySlot?.Itemstack?.Clone());
        }
    }
}