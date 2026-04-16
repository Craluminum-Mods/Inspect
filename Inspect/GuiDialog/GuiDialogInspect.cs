using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Inspect;

public class GuiDialogInspect : GuiDialog
{
    public static bool LockStack { get; private set; } = false;
    private static ItemStack? forStack;
    private static long? forEntityId;
    public static bool EntityGuiTransformPatchActive { get; private set; }
    public static float EntityGuiPitchDeg { get; private set; }

    public const int DEFAULT_ROTATION_DELAY_IN_MS = 1500;
    public const float DEFAULT_ZOOM = 4f;
    public const float MOUSE_SENSITIVITY = 0.4f;

    Vec4f lightPos = new Vec4f(-1, -1, 0, 0).NormalizeXYZ();
    Matrixf mat = new();

    protected ElementBounds? insetSlotBounds;

    protected bool showTooltip = true;

    protected float currentZoom = DEFAULT_ZOOM;
    protected float targetZoom = DEFAULT_ZOOM;

    protected bool offsetObject;
    protected float offsetX;
    protected float offsetY;

    protected bool rotateObject;
    protected float rotX;
    protected float rotY;
    protected float rotZ;
    protected bool autoRotation = true;
    protected bool pauseAutoRotation = false;
    protected bool toggleAnimations = true;
    protected int? rotationDelayInMs;

    public override float ZSize => 999;
    public override double DrawOrder => 0.889;
    public override string? ToggleKeyCombinationCode => null;

    public override bool PrefersUngrabbedMouse => true;
    public override bool DisableMouseGrab => true;
    public override double InputOrder => 0;
    public override bool CaptureAllInputs() => true;

    private readonly Dictionary<string, Func<bool>> dialogHotkeys;

    public GuiDialogInspect(ICoreClientAPI capi) : base(capi)
    {
        (capi.World as ClientMain)?.Platform.WindowResized += Platform_WindowResized;

        dialogHotkeys = new()
        {
            { "inspect:hidetooltip", OnHideTooltip },
            { "inspect:reset", OnResetValues },
            { "inspect:zoom-in", OnZoomIn },
            { "inspect:zoom-out", OnZoomOut },
            { "inspect:autorotate", OnToggleAutoRotate },
            { "inspect:animations", OnToggleAnimations },
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        (capi.World as ClientMain)?.Platform.WindowResized -= Platform_WindowResized;
    }

    private void Platform_WindowResized(int nowWidth, int nowHeight) => ComposeGuis();

    protected void ComposeGuis()
    {
        double w = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        double h = capi.Render.FrameHeight / RuntimeEnv.GUIScale;

        ElementBounds dialogBounds = ElementBounds.Fixed(0, 0, w, h).WithAlignment(EnumDialogArea.LeftTop);
        ElementBounds childBounds = ElementBounds.Fixed(0, 0, w, h);
        insetSlotBounds = ElementBounds.Fixed(0, 0, w, h).WithAlignment(EnumDialogArea.LeftTop);

        ElementBounds tooltipBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding * 2, GuiStyle.ElementToDialogPadding * 2, 1000, 0).WithAlignment(EnumDialogArea.LeftTop);

        GuiComposer composer;
        Composers["inspect"] = composer = capi.Gui
            .CreateCompo("inspect", dialogBounds)
            .BeginChildElements(childBounds);

        double[] bgColor = GuiStyle.DialogDefaultBgColor;
        bgColor[3] = 0.65f;
        composer.AddGameOverlay(insetSlotBounds, bgColor);

        StringBuilder tooltipText = new();
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-hidetooltip"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-reset"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-rotate"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-move"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-zoom", Lang.Get("inspect:key-mouse-wheel")));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-zoom-in"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-zoom-out"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-autorotate"));
        tooltipText.AppendLine(Lang.Get("inspect:tooltip-animations"));

        composer.AddIf(showTooltip);
        composer.AddRichtext(tooltipText.ToString(), CairoFont.WhiteMediumText().WithFontSize(24), tooltipBounds, "tooltip");
        composer.EndIf();
        composer.Compose();
        composer?.GetRichtext("tooltip")?.CalcHeightAndPositions();
    }

    public static void SetStack(ItemStack? fromStack)
    {
        if (!LockStack && fromStack != null)
        {
            forStack = fromStack.Clone();
        }
    }

    public override void OnGuiOpened()
    {
        showTooltip = true;
        ComposeGuis();
        LockStack = true;
    }

    public override void OnGuiClosed()
    {
        forStack = null;
        forEntityId = null;
        EntityGuiTransformPatchActive = false;
        EntityGuiPitchDeg = 0f;
        ResetValues();
        ResetAutoRotation();
        LockStack = false;
    }

    private void ResetValues()
    {
        currentZoom = DEFAULT_ZOOM;
        targetZoom = DEFAULT_ZOOM;
        rotX = 0f;
        rotY = 0f;
        rotZ = 0f;
        offsetX = 0f;
        offsetY = 0f;
        EntityGuiPitchDeg = 0f;
        rotateObject = false;
        offsetObject = false;
    }

    private void ResetAutoRotation()
    {
        pauseAutoRotation = false;
        autoRotation = true;
        rotationDelayInMs = null;
    }

    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        targetZoom = GameMath.Clamp(targetZoom + args.deltaPrecise / 2f, 0.5f, 10f);
        args.SetHandled(true);
    }

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);

        if (insetSlotBounds?.PointInside(args.X, args.Y) == true)
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
            pauseAutoRotation = true;
            rotationDelayInMs = DEFAULT_ROTATION_DELAY_IN_MS;

            if ((args.Modifiers & 1) != 0)
            {
                rotZ -= args.DeltaX * MOUSE_SENSITIVITY;
            }
            else
            {
                rotY -= args.DeltaX * MOUSE_SENSITIVITY;
            }

            rotX -= args.DeltaY * MOUSE_SENSITIVITY;
        }

        if (offsetObject)
        {
            pauseAutoRotation = true;
            rotationDelayInMs = DEFAULT_ROTATION_DELAY_IN_MS;

            offsetX += args.DeltaX;
            offsetY += args.DeltaY;
        }
    }

    public override void OnKeyDown(KeyEvent args)
    {
        base.OnKeyDown(args);

        HotKey dialogHotkey = capi.Input.GetHotKeyByCode("inspect:toggle");
        if (dialogHotkey != null && dialogHotkey.DidPress(args, capi.World, capi.World.Player, allowCharacterControls: true) && TryClose())
        {
            args.Handled = true;
            return;
        }

        foreach ((string hotkeyCode, Func<bool> func) in dialogHotkeys)
        {
            HotKey hotkey = capi.Input.GetHotKeyByCode(hotkeyCode);
            if (hotkey == null) continue;

            if (hotkey.DidPress(args, capi.World, capi.World.Player, allowCharacterControls: true) && func.Invoke())
            {
                args.Handled = true;
                return;
            }
            else if (hotkey.FallbackDidPress(args, capi.World, capi.World.Player, allowCharacterControls: true) && func.Invoke())
            {
                args.Handled = true;
                return;
            }
        }
    }

    private bool OnHideTooltip()
    {
        showTooltip = !showTooltip;
        ComposeGuis();
        return true;
    }

    private bool OnResetValues()
    {
        ResetValues();
        return true;
    }

    private bool OnZoomIn()
    {
        targetZoom = GameMath.Clamp(targetZoom + 1f, 0.5f, 10f);
        return true;
    }

    private bool OnZoomOut()
    {
        targetZoom = GameMath.Clamp(targetZoom - 1f, 0.5f, 10f);
        return true;
    }

    private bool OnToggleAutoRotate()
    {
        autoRotation = !autoRotation;
        pauseAutoRotation = false;
        rotationDelayInMs = null;
        return true;
    }

    private bool OnToggleAnimations()
    {
        toggleAnimations = !toggleAnimations;
        return true;
    }

    public override void OnBlockTexturesLoaded()
    {
        base.OnBlockTexturesLoaded();

        capi.Input.SetHotKeyHandler("inspect:toggle", ToggleGui);
        capi.Input.SetHotKeyHandler("inspect:toggle-for-block", ToggleGuiForSelectedBlock);
    }

    public bool ToggleGui(KeyCombination k)
    {
        if (IsOpened())
        {
            Toggle();
            return true;
        }

        if (forStack != null)
        {
            TryOpen();
            return true;
        }

        ClientMain game = (ClientMain)capi.World;
        BlockSelection? tempBlockSel = null;
        EntitySelection? tempEntitySel = null;
        game.RayTraceForSelection(game.player.Entity.Pos.XYZ.Add(game.player.Entity.LocalEyePos), game.player.Entity.Pos.Pitch, game.player.Entity.Pos.Yaw, 100, ref tempBlockSel, ref tempEntitySel, null, null);
        if (tempEntitySel != null)
        {
            forEntityId = tempEntitySel.Entity.EntityId;
            TryOpen();
            return true;
        }

        if (!capi.World.Player.InventoryManager.ActiveHotbarSlot.Empty)
        {
            forStack = capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Clone();
            TryOpen();
            return true;
        }

        return false;
    }

    public bool ToggleGuiForSelectedBlock(KeyCombination k)
    {
        BlockSelection blockSel = capi.World.Player.CurrentBlockSelection;
        if (blockSel == null)
        {
            return false;
        }

        if (capi.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGroundStorage begs && begs.GetSlotAt(blockSel) is ItemSlot slot && !slot.Empty)
        {
            forStack = slot.Itemstack.Clone();
            Toggle();
            return true;
        }
        else
        {
            ItemStack? blockStack = blockSel.Block.OnPickBlock(capi.World, blockSel.Position)?.Clone();
            if (blockStack != null)
            {
                forStack = blockStack;
                Toggle();
                return true;
            }
        }

        return false;
    }

    public override void OnRenderGUI(float deltaTime)
    {
        base.OnRenderGUI(deltaTime);

        if (insetSlotBounds == null) return;

        currentZoom += (targetZoom - currentZoom) * deltaTime * 5f;

        if (pauseAutoRotation && rotationDelayInMs != null)
        {
            rotationDelayInMs -= (int)(deltaTime * 1000);

            if (rotationDelayInMs < 0)
            {
                rotationDelayInMs = null;
                pauseAutoRotation = false;
            }
        }

        if (autoRotation && !pauseAutoRotation)
        {
            rotY += deltaTime * 20f;
            rotY %= 360f;

            if (forEntityId == null)
            {
                rotX += (float)Math.Cos(capi.InWorldEllapsedMilliseconds / 1000f) * deltaTime * 5f;
                rotX %= 360f;
            }
        }

        mat.Identity().RotateXDeg(-14);
        Vec4f lightRot = mat.TransformVector(lightPos);
        capi.Render.CurrentActiveShader.Uniform("lightPosition", lightRot.X, lightRot.Y, lightRot.Z);

        var frameWidth = capi.Render.FrameWidth * 0.5f;
        var frameHeight = capi.Render.FrameHeight * 0.5f;

        float centerX = (float)offsetX + frameWidth;
        float centerY = (float)offsetY + frameHeight;
        float posZ = (float)GuiElement.scaled(250);
        float size = (float)GuiElement.scaled(100 * currentZoom) / RuntimeEnv.GUIScale;
        float entitySize = size;

        Entity? forEntity = forEntityId != null ? capi.World.GetEntityById(forEntityId.Value) : null;

        if (forEntity != null && forEntity.AnimManager?.Animator != null)
        {
            capi.Render.CurrentActiveShader.Uniform("applyAnimation", toggleAnimations ? (int)1 : 0);
            capi.Render.CurrentActiveShader.UBOs["Animation"].Update(forEntity.AnimManager.Animator.Matrices, 0, forEntity.AnimManager.Animator.MaxJointId * 16 * 4);

            forStack = null;

            capi.Render.GlPushMatrix();
            capi.Render.GlRotate(-14, 1, 0, 0);

            float entityHeight = forEntity.SelectionBox.Y2 - forEntity.SelectionBox.Y1;
            float entityWidth = forEntity.SelectionBox.X2 - forEntity.SelectionBox.X1;
            float entityDepth = forEntity.SelectionBox.Z2 - forEntity.SelectionBox.Z1;
            float entityMaxDimension = Math.Max(entityHeight, Math.Max(entityWidth, entityDepth));
            float entityScaledHeight = entityHeight * forEntity.Properties.Client.Size * entitySize;
            float entityPosZ = Math.Max((float)GuiElement.scaled(250), entitySize * forEntity.Properties.Client.Size * entityMaxDimension * 2f);
            float entityYaw = -GameMath.PIHALF + 0.3f + rotY * GameMath.DEG2RAD;
            float entityDrawY = centerY - 2 * entitySize;
            EntityGuiPitchDeg = rotX;
            EntityGuiTransformPatchActive = true;

            try
            {
                capi.Render.RenderEntityToGui(
                    deltaTime,
                    forEntity,
                    centerX - entitySize,
                    entityDrawY,
                    entityPosZ,
                    entityYaw,
                    entitySize,
                    ColorUtil.WhiteArgb
                );
            }
            finally
            {
                EntityGuiTransformPatchActive = false;
            }

            capi.Render.GlPopMatrix();
            capi.Render.CurrentActiveShader.Uniform("applyAnimation", (int)0);
        }
        else if (forStack != null)
        {
            capi.Render.PushScissor(insetSlotBounds);

            ClientMain game = (ClientMain)capi.World;
            ItemRenderInfo renderInfo = InventoryItemRenderer.GetItemStackRenderInfo(game, new DummySlot(forStack), EnumItemRenderTarget.Gui, deltaTime);

            if (renderInfo.ModelRef != null)
            {
                if (autoRotation && !pauseAutoRotation)
                {
                    forStack.Collectible.InGuiIdle(game, forStack);
                }

                ModelTransform transform = renderInfo.Transform;
                bool upsideDown = forStack.Class == EnumItemClass.Block;

                float itemOffsetX = forStack.Class == EnumItemClass.Item ? 3f : 0f;
                float itemOffsetY = forStack.Class == EnumItemClass.Item ? 1f : 0f;
                float originX = (float)(transform.Origin.X + GuiElement.scaled(transform.Translation.X));
                float originY = (float)(transform.Origin.Y + GuiElement.scaled(transform.Translation.Y));
                float originZ = (float)(transform.Origin.Z * size + GuiElement.scaled(transform.Translation.Z));

                mat.Identity();
                mat.Translate(centerX - itemOffsetX - originX, centerY - itemOffsetY - originY, posZ);
                mat.Translate(originX, originY, originZ);

                mat.Scale(size * transform.ScaleXYZ.X, size * transform.ScaleXYZ.Y, size * transform.ScaleXYZ.Z);

                mat.RotateXDeg(transform.Rotation.X + rotX + (upsideDown ? 180f : 0));
                mat.RotateYDeg(transform.Rotation.Y + rotY);
                mat.RotateZDeg(transform.Rotation.Z + rotZ);

                mat.Translate(-transform.Origin.X, -transform.Origin.Y, -transform.Origin.Z);

                var shader = game.guiShaderProg;
                shader.NormalShaded = renderInfo.NormalShaded ? 1 : 0;
                shader.RgbaIn = new Vec4f(1, 1, 1, 1);
                shader.ApplyColor = renderInfo.ApplyColor ? 1 : 0;
                shader.AlphaTest = renderInfo.AlphaTest;
                shader.ModelMatrix = mat.Values;
                shader.ProjectionMatrix = game.CurrentProjectionMatrix;
                shader.ModelViewMatrix = mat.ReverseMul(game.CurrentModelViewMatrix).Values;
                shader.ApplyModelMat = 1;

                capi.Render.RenderMultiTextureMesh(renderInfo.ModelRef, "tex2d");

                shader.ApplyModelMat = 0;
            }
            capi.Render.PopScissor();
        }

        capi.Render.CurrentActiveShader.Uniform("lightPosition", GameMath.ONEOVERROOT2, -GameMath.ONEOVERROOT2, 0f);
    }
}