using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Inspect;

public class GuiDialogInspect : GuiDialog
{
    Vec4f lighPos = new Vec4f(-1, -1, 0, 0).NormalizeXYZ();
    Matrixf mat = new Matrixf();

    protected ElementBounds insetSlotBounds;

    protected bool rotateObject;
    protected float charZoom = 1f;
    protected float rotX = 0f;
    protected float rotY = 0f;
    protected float rotZ = 0f;

    public override float ZSize => (float)GuiElement.scaled(999);

    public GuiDialogInspect(ICoreClientAPI capi) : base(capi)
    {
        (capi.World as ClientMain)?.Platform.WindowResized += Platform_WindowResized;
    }

    private void Platform_WindowResized(int nowWidth, int nowHeight) => ComposeGuis();

    protected void ComposeGuis()
    {
        double w = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        double h = capi.Render.FrameHeight / RuntimeEnv.GUIScale;

        ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, w, h).WithAlignment(EnumDialogArea.LeftTop);
        ElementBounds childBounds = ElementBounds.Fixed(0, 0, w, h);
        insetSlotBounds = ElementBounds.Fixed(0, 0, w, h).WithAlignment(EnumDialogArea.LeftTop);

        GuiComposer composer;
        Composers["inspect"] = composer = capi.Gui
            .CreateCompo("inspect", dialogBounds)
            .BeginChildElements(childBounds);

        double[] bgColor = GuiStyle.DialogDefaultBgColor;
        bgColor[3] = 0.65f;

        composer.AddGameOverlay(insetSlotBounds, bgColor);
        composer.Compose();
    }

    public override void OnGuiOpened() => ComposeGuis();

    public override void OnGuiClosed()
    {
        charZoom = 5f;
        rotX = 0f;
        rotY = 0f;
        rotZ = 0f;
        rotateObject = false;
    }

    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);

        charZoom = GameMath.Clamp(charZoom + args.deltaPrecise / 5f, 0.5f, 10f);
        args.SetHandled(true);
    }

    public override bool PrefersUngrabbedMouse => false;

    #region Render

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);
        rotateObject = insetSlotBounds.PointInside(args.X, args.Y);
    }

    public override void OnMouseUp(MouseEvent args)
    {
        base.OnMouseUp(args);
        rotateObject = false;
    }

    public override void OnMouseMove(MouseEvent args)
    {
        base.OnMouseMove(args);
        if (rotateObject)
        {
            bool shiftPressed = (args.Modifiers & 1) != 0;
            float sensitivity = 0.4f;

            if (shiftPressed)
            {
                rotZ -= args.DeltaX * sensitivity;
            }
            else
            {
                rotY -= args.DeltaX * sensitivity;
            }
            rotX -= args.DeltaY * sensitivity;
        }
    }

    public override void OnRenderGUI(float deltaTime)
    {
        base.OnRenderGUI(deltaTime);

        capi.Render.GlPushMatrix();

        mat.Identity().RotateXDeg(-14);
        Vec4f lightRot = mat.TransformVector(lighPos);
        capi.Render.CurrentActiveShader.Uniform("lightPosition", lightRot.X, lightRot.Y, lightRot.Z);

        double w = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        double h = capi.Render.FrameHeight / RuntimeEnv.GUIScale;

        float posX = (float)((w / 2) - GuiElement.scaled(150));
        float posY = (float)((h / 2) - GuiElement.scaled(150));
        float posZ = (float)GuiElement.scaled(9999);
        float size = (float)GuiElement.scaled(100 * charZoom);

        capi.Render.GlTranslate(posX, posY, posZ);

        capi.Render.GlRotate(rotX, 1, 0, 0);
        capi.Render.GlRotate(rotY, 0, 1, 0);
        capi.Render.GlRotate(rotZ, 0, 0, 1);

        capi.Render.PushScissor(insetSlotBounds);

        capi.Render.RenderItemstackToGui(
            capi.World.Player.InventoryManager.ActiveHotbarSlot,
            0, 0, 0,
            size,
            ColorUtil.WhiteArgb,
            showStackSize: false
        );

        capi.Render.PopScissor();

        capi.Render.CurrentActiveShader.Uniform("lightPosition", GameMath.ONEOVERROOT2, -GameMath.ONEOVERROOT2, 0f);
        capi.Render.GlPopMatrix();
    }
    #endregion

    public override string ToggleKeyCombinationCode => "inspect";
}