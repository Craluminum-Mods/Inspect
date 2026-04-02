using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Inspect;

public class Core : ModSystem
{
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Input.RegisterHotKey("inspect", Lang.Get("inspect:hotkey-inspect"), GlKeys.V, HotkeyType.GUIOrOtherControls);
        api.Gui.RegisterDialog(new GuiDialogInspect(api));
    }
}