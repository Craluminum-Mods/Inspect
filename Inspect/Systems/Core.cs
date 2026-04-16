using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Inspect.Systems;

public class Core : ModSystem
{
    private Harmony HarmonyInstance => new(Mod.Info.ModID);

    public override bool ShouldLoad(EnumAppSide forSide) => forSide.IsClient();

    public override void StartPre(ICoreAPI api)
    {
        HarmonyInstance.PatchAllUncategorized();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Input.RegisterHotKey("inspect:toggle",              Lang.Get("inspect:hotkey-toggle"), GlKeys.V, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:toggle-for-block",    Lang.Get("inspect:hotkey-toggle-for-block"), GlKeys.V, HotkeyType.GUIOrOtherControls, shiftPressed: true);
        api.Input.RegisterHotKey("inspect:hidetooltip",         Lang.Get("inspect:hotkey-hidetooltip"), GlKeys.LShift, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:reset",               Lang.Get("inspect:hotkey-reset"), GlKeys.Space, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:zoom-in",             Lang.Get("inspect:hotkey-zoom-in"), GlKeys.Plus, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:zoom-out",            Lang.Get("inspect:hotkey-zoom-out"), GlKeys.Minus, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:autorotate",          Lang.Get("inspect:hotkey-autorotate"), GlKeys.R, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:animations",          Lang.Get("inspect:hotkey-animations"), GlKeys.A, HotkeyType.GUIOrOtherControls);
        api.Gui.RegisterDialog(new GuiDialogInspect(api));
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }
}
