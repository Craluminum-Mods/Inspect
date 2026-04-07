using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Inspect;

public class Core : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Input.RegisterHotKey("inspect:toggle",      Lang.Get("inspect:hotkey-toggle"), GlKeys.V, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:hidetooltip", Lang.Get("inspect:hotkey-hidetooltip"), GlKeys.LShift, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:reset",       Lang.Get("inspect:hotkey-reset"), GlKeys.Space, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:zoom-in",     Lang.Get("inspect:hotkey-zoom-in"), GlKeys.Plus, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:zoom-out",    Lang.Get("inspect:hotkey-zoom-out"), GlKeys.Minus, HotkeyType.GUIOrOtherControls);
        api.Input.RegisterHotKey("inspect:autorotate",  Lang.Get("inspect:hotkey-autorotate"), GlKeys.R, HotkeyType.GUIOrOtherControls);
        api.Gui.RegisterDialog(new GuiDialogInspect(api));
    }
}