using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Inspect.HarmonyPatches;

[HarmonyPatch(typeof(GuiElementItemSlotGridBase), nameof(GuiElementItemSlotGridBase.RenderInteractiveElements))]
public static class GuiElementItemSlotGridBase_Patch
{
    [HarmonyPostfix]
    public static void Postfix(GuiElementItemSlotGridBase __instance, float deltaTime, ICoreClientAPI ___api, IInventory ___inventory)
    {
        if (GuiDialogInspect.LockStack) return;

        if (__instance.hoverSlotId != -1 && ___inventory[__instance.hoverSlotId] != null)
        {
            GuiDialogInspect.SetStack(___inventory[__instance.hoverSlotId]?.Itemstack?.Clone());
        }
    }
}