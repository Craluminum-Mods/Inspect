using HarmonyLib;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Inspect.HarmonyPatches;

[HarmonyPatch(typeof(EntityShapeRenderer), "loadModelMatrixForGui")]
public static class EntityShapeRenderer_LoadModelMatrixForGui_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(EntityShapeRenderer __instance, Entity entity, double posX, double posY, double posZ, double yawDelta, float size)
    {
        if (!GuiDialogInspect.EntityGuiTransformPatchActive)
        {
            return true;
        }

        Mat4f.Identity(__instance.ModelMat);
        Mat4f.Translate(__instance.ModelMat, __instance.ModelMat, (float)posX, (float)posY, (float)posZ);
        Mat4f.Translate(__instance.ModelMat, __instance.ModelMat, size, 2 * size, 0);

        float rotX = entity.Properties.Client.Shape != null ? entity.Properties.Client.Shape.rotateX : 0;
        float rotY = entity.Properties.Client.Shape != null ? entity.Properties.Client.Shape.rotateY : 0;
        float rotZ = entity.Properties.Client.Shape != null ? entity.Properties.Client.Shape.rotateZ : 0;

        Mat4f.RotateX(__instance.ModelMat, __instance.ModelMat, GameMath.PI + (rotX + GuiDialogInspect.EntityGuiPitchDeg) * GameMath.DEG2RAD);
        Mat4f.RotateY(__instance.ModelMat, __instance.ModelMat, (float)yawDelta + rotY * GameMath.DEG2RAD);
        Mat4f.RotateZ(__instance.ModelMat, __instance.ModelMat, rotZ * GameMath.DEG2RAD);

        float scale = entity.Properties.Client.Size * size;
        Mat4f.Scale(__instance.ModelMat, __instance.ModelMat, new float[] { scale, scale, scale });
        Mat4f.Translate(__instance.ModelMat, __instance.ModelMat, -0.5f, 0f, -0.5f);
        return false;
    }
}
