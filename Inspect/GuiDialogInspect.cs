using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
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
    protected bool offsetObject;
    protected float charZoom = 2f;
    protected float rotX;
    protected float rotY;
    protected float rotZ;
    protected float? offsetX;
    protected float? offsetY;
    protected bool showTooltip = true;
    protected bool autoRotation = true;
    protected float? autoRotationDelayInMs;

    public const float AUTO_ROTATION_DELAY_IN_MS = 1500;

    public override float ZSize => (float)GuiElement.scaled(999);

    public override string ToggleKeyCombinationCode => "inspect";
    
    public override bool CaptureAllInputs() => true;

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

        ElementBounds tooltipBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding * 2, GuiStyle.ElementToDialogPadding * 2, 0, 0).WithAlignment(EnumDialogArea.LeftTop);

        GuiComposer composer;
        Composers["inspect"] = composer = capi.Gui
            .CreateCompo("inspect", dialogBounds)
            .BeginChildElements(childBounds);

        double[] bgColor = GuiStyle.DialogDefaultBgColor;
        bgColor[3] = 0.65f;
        composer.AddGameOverlay(insetSlotBounds, bgColor);

        StringBuilder topText = new();
        topText.AppendLine(Lang.Get("inspect:controls-hidetooltip"));
        topText.AppendLine(Lang.Get("inspect:controls-reset"));
        topText.AppendLine(Lang.Get("inspect:controls-rotate"));
        topText.AppendLine(Lang.Get("inspect:controls-move"));
        topText.AppendLine(Lang.Get("inspect:controls-zoom"));
        topText.AppendLine(Lang.Get("inspect:controls-autorotate"));

        composer.AddIf(showTooltip);
        composer.AddStaticTextAutoBoxSize(topText.ToString(), CairoFont.WhiteMediumText().WithFontSize(24), EnumTextOrientation.Left, tooltipBounds, "tooltip");
        composer.EndIf();

        composer.Compose();
    }

    public override void OnGuiOpened() => ComposeGuis();

    public override void OnGuiClosed()
    {
        ResetValues();
        ResetAutoRotation();
    }

    private void ResetValues()
    {
        charZoom = 4f;
        rotX = 0f;
        rotY = 0f;
        rotZ = 0f;
        offsetX = 0f;
        offsetY = 0f;
        rotateObject = false;
        showTooltip = true;
    }

    private void ResetAutoRotation()
    {
        autoRotation = true;
        autoRotationDelayInMs = null;
    }
    
    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        charZoom = GameMath.Clamp(charZoom + args.deltaPrecise / 5f, 0.5f, 10f);
        args.SetHandled(true);
    }

    public override bool PrefersUngrabbedMouse => false;

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);

        if (insetSlotBounds.PointInside(args.X, args.Y))
        {
            switch (args.Button)
            {
                case EnumMouseButton.Left:
                    rotateObject = true;
                    break;
                case EnumMouseButton.Right:
                    offsetObject = true;
                    break;
            }
        }
    }

    public override void OnMouseUp(MouseEvent args)
    {
        base.OnMouseUp(args);
        rotateObject = false;
        offsetObject = false;
    }

    public override void OnMouseMove(MouseEvent args)
    {
        base.OnMouseMove(args);

        if (rotateObject)
        {
            autoRotation = false;
            autoRotationDelayInMs = AUTO_ROTATION_DELAY_IN_MS;

            float sensitivity = 0.4f;

            if ((args.Modifiers & 1) != 0)
            {
                rotZ -= args.DeltaX * sensitivity;
            }
            else
            {
                rotY -= args.DeltaX * sensitivity;
            }

            rotX -= args.DeltaY * sensitivity;
        }

        if (offsetObject)
        {
            autoRotation = false;
            autoRotationDelayInMs = AUTO_ROTATION_DELAY_IN_MS;

            offsetX += args.DeltaX;
            offsetY += args.DeltaY;
        }
    }

    public override void OnKeyPress(KeyEvent args)
    {
        base.OnKeyPress(args);

        int glKey = KeyConverter.NewKeysToGlKeys[args.KeyCode];

        switch (glKey)
        {
            case (int)GlKeys.Space:
                ResetValues();
                args.Handled = true;
                break;
        }
    }

    public override void OnKeyDown(KeyEvent args)
    {
        base.OnKeyDown(args);

        if (args.ShiftPressed)
        {
            showTooltip = !showTooltip;
            ComposeGuis();
            args.Handled = true;
        }

        switch (args.KeyCode)
        {
            case (int)GlKeys.R:
                autoRotation = !autoRotation;
                autoRotationDelayInMs = null;
                args.Handled = true;
                break;
        }
    }

    public override void OnKeyUp(KeyEvent args)
    {
        base.OnKeyUp(args);
    }

    public override void OnRenderGUI(float deltaTime)
    {
        base.OnRenderGUI(deltaTime);

        if (autoRotationDelayInMs != null)
        {
            autoRotationDelayInMs -= deltaTime * 1000;
            
            if (autoRotationDelayInMs < 0)
            {
                autoRotationDelayInMs = null;
                autoRotation = true;
            }
        }

        if (autoRotation)
        {
            rotY += deltaTime * 20f;
            rotX = 10f * (float)Math.Sin(capi.InWorldEllapsedMilliseconds / 1000f);
        }

        mat.Identity().RotateXDeg(-14);
        Vec4f lightRot = mat.TransformVector(lighPos);
        capi.Render.CurrentActiveShader.Uniform("lightPosition", lightRot.X, lightRot.Y, lightRot.Z);

        var frameWidth = capi.Render.FrameWidth * 0.5f;
        var frameHeight = capi.Render.FrameHeight * 0.5f;

        float centerX = offsetX != null ? offsetX.Value + frameWidth : frameWidth;
        float centerY = offsetY != null ? offsetY.Value + frameHeight : frameHeight;
        float posZ = (float)GuiElement.scaled(9999);
        float size = (float)GuiElement.scaled(100 * charZoom);

        capi.Render.PushScissor(insetSlotBounds);

        ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
        ItemStack itemstack = slot.Itemstack;

        if (itemstack != null)
        {
            ClientMain game = (ClientMain)capi.World;
            ItemRenderInfo renderInfo = InventoryItemRenderer.GetItemStackRenderInfo(game, slot, EnumItemRenderTarget.Gui, deltaTime);

            if (renderInfo.ModelRef != null)
            {
                itemstack.Collectible.InGuiIdle(game, itemstack);

                ModelTransform transform = renderInfo.Transform;
                bool upsideDown = itemstack.Class == EnumItemClass.Block;

                float itemOffsetX = itemstack.Class == EnumItemClass.Item ? 3f : 0f;
                float itemOffsetY = itemstack.Class == EnumItemClass.Item ? 1f : 0f;
                float originX = (float)(transform.Origin.X + GuiElement.scaled(transform.Translation.X));
                float originY = (float)(transform.Origin.Y + GuiElement.scaled(transform.Translation.Y));
                float originZ = (float)(transform.Origin.Z * size + GuiElement.scaled(transform.Translation.Z));

                mat.Identity();
                mat.Translate(
                    centerX - itemOffsetX - originX,
                    centerY - itemOffsetY - originY,
                    posZ
                );
                mat.Translate(originX, originY, originZ);

                mat.Scale(
                    size * transform.ScaleXYZ.X,
                    size * transform.ScaleXYZ.Y,
                    size * transform.ScaleXYZ.Z
                );
                mat.RotateXDeg(transform.Rotation.X + rotX + (upsideDown ? 180f : 0));
                mat.RotateYDeg(transform.Rotation.Y + rotY);
                mat.RotateZDeg(transform.Rotation.Z + rotZ);
                mat.Translate(-transform.Origin.X, -transform.Origin.Y, -transform.Origin.Z);

                var shader = game.guiShaderProg;
                shader.NormalShaded = renderInfo.NormalShaded ? 1 : 0;
                shader.RgbaIn = new Vec4f(1, 1, 1, 1);
                shader.ApplyColor = renderInfo.ApplyColor ? 1 : 0;
                shader.AlphaTest = renderInfo.AlphaTest;
                shader.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
                shader.ModelMatrix = mat.Values;
                shader.ProjectionMatrix = game.CurrentProjectionMatrix;
                shader.ModelViewMatrix = mat.ReverseMul(game.CurrentModelViewMatrix).Values;
                shader.ApplyModelMat = 1;

                capi.Render.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d");

                shader.ApplyModelMat = 0;
                shader.NormalShaded = 0;
                shader.AlphaTest = 0f;

            }
        }

        capi.Render.PopScissor();
        capi.Render.CurrentActiveShader.Uniform("lightPosition", GameMath.ONEOVERROOT2, -GameMath.ONEOVERROOT2, 0f);
    }
}