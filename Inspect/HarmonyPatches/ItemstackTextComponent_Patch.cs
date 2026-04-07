using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Inspect.HarmonyPatches;

[HarmonyPatch(typeof(ItemstackTextComponent), nameof(ItemstackTextComponent.RenderInteractiveElements))]
public static class ItemstackTextComponent_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ItemstackTextComponent __instance, float deltaTime, double renderX, double renderY, double renderZ, ICoreClientAPI ___capi, DummySlot ___slot)
    {
        if (GuiDialogInspect.lockStack) return;

        LineRectangled bounds = __instance.BoundsPerLine[0];
        int relx = (int)(___capi.Input.MouseX - renderX);
        int rely = (int)(___capi.Input.MouseY - renderY);

        if (bounds.PointInside(relx, rely))
        {
            GuiDialogInspect.forStack = (___slot?.Itemstack?.Clone());
        }
    }
}