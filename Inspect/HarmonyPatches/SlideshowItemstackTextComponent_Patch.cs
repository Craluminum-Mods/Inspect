using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Inspect.HarmonyPatches;

[HarmonyPatch(typeof(SlideshowItemstackTextComponent), nameof(SlideshowItemstackTextComponent.RenderInteractiveElements))]
public static class SlideshowItemstackTextComponent_Patch
{
    [HarmonyPostfix]
    public static void Postfix(SlideshowItemstackTextComponent __instance, float deltaTime, double renderX, double renderY, double renderZ, ICoreClientAPI ___capi, DummySlot ___slot)
    {
        if (GuiDialogInspect.lockStack) return;

        LineRectangled bounds = __instance.BoundsPerLine[0];
        int relx = (int)(___capi.Input.MouseX - renderX + __instance.renderOffset.X);
        int rely = (int)(___capi.Input.MouseY - renderY + __instance.renderOffset.Y);
 
        if (bounds.PointInside(relx, rely))
        {
            GuiDialogInspect.forStack = (___slot?.Itemstack?.Clone());
        }
    }
}