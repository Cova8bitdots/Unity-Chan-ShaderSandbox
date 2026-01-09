using UnityEditor;
using UnityEngine;

public sealed class HoloToonShaderGUI : ShaderGUI
{
    // ---- Foldout states (Editor-only) ----
    private static bool s_foldSurface = true;
    private static bool s_foldToon = true;
    private static bool s_foldSpec = false;
    private static bool s_foldRim = true;
    private static bool s_foldHolo = true;

    // ---- Helpers ----
    private static MaterialProperty Find(string name, MaterialProperty[] props, bool mandatory = false)
        => FindProperty(name, props, mandatory);

    private static bool GetToggle(MaterialProperty prop)
        => prop != null && prop.floatValue > 0.5f;

    private static void SetToggle(MaterialProperty prop, bool value)
    {
        if (prop == null) return;
        prop.floatValue = value ? 1f : 0f;
    }

    private static void Header(string title)
    {
        GUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    public override void OnGUI(MaterialEditor me, MaterialProperty[] props)
    {
        var mat = me.target as Material;
        if (mat == null)
        {
            base.OnGUI(me, props);
            return;
        }

        // ---- Property lookup (MUST match your ShaderGraph Blackboard "Reference" names) ----
        // Surface
        var pBaseMap = Find("_BaseMap", props);
        var pBaseColor = Find("_BaseColor", props);

        // Toon shading 1st / 2nd
        var pApply1 = Find("_Apply1stShade", props);
        var pShade1Color = Find("_Shadow1stColor", props);
        var pT1 = Find("_Shade1stThreshold", props);
        var pF1 = Find("_Shade1stFeather", props);

        var pApply2 = Find("_Apply2ndShade", props);
        var pShade2Color = Find("_Shadow2ndColor", props);
        var pT2 = Find("_Shade2ndThreshold", props);
        var pF2 = Find("_Shade2ndFeather", props);

        var pRampPower = Find("_RampPower", props);
        var pRampStrength = Find("_RampStrength", props);

        // Specular (new)
        var pApplySpec = Find("_ApplySpecular", props, false);
        var pSpecColor = Find("_SpecColor", props, false);
        var pSpecPower = Find("_SpecPower", props, false);
        var pSpecThreshold = Find("_SpecThreshold", props, false);
        var pSpecFeather = Find("_SpecFeather", props, false);

        // Rim
        var pRimColor = Find("_RimColor", props);
        var pRimPower = Find("_RimPower", props);
        var pRimSoftness = Find("_RimSoftness", props);
        var pRimWidth = Find("_RimWidth", props);

        // Hologram
        // NOTE: You are using a float toggle property (not keyword) here.
        // If you later switch to a keyword, this must be controlled via EnableKeyword/DisableKeyword instead.
        var pHoloEnable = Find("_HOLO_ENABLE", props, false);
        var pHoloColor = Find("_HoloColor", props, false);
        var pHoloDarkColor = Find("_HoloDarkColor", props, false);
        var pHoloTiling = Find("_HoloTilingPerMeter", props, false);
        var pHoloIntensity = Find("_HoloIntensity", props, false);
        var pHoloDarkIntensity = Find("_HoloDarkIntensity", props, false);
        var pHoloScrollSpeed = Find("_HoloScrollSpeed", props, false);
        var pHoloBorderWidth = Find("_HoloBorderWidth", props, false);
        var pHoloSoftness = Find("_HoloSoftness", props, false);
        var pHoloBaseY = Find("_HoloBaseY", props, false);
        var pHoloPhaseOffset = Find("_HoloPhaseOffset", props, false);

        EditorGUI.BeginChangeCheck();

        // Surface
        s_foldSurface = DrawUnityHeader(s_foldSurface, "Surface Inputs");
        if (s_foldSurface)
        {
            if (pBaseMap != null)
            {
                me.TexturePropertySingleLine(new GUIContent("Base Map"), pBaseMap, pBaseColor);
            }
            else if (pBaseColor != null)
            {
                me.ColorProperty(pBaseColor, "Base Color");
            }
        }

        // Toon
        s_foldToon = DrawUnityHeader(s_foldToon, "Toon Shading");
        if (s_foldToon)
        {
            // 1st
            if (pApply1 != null)
            {
                bool apply1 = EditorGUILayout.Toggle("Apply to 1st", GetToggle(pApply1));
                SetToggle(pApply1, apply1);

                using (new EditorGUI.IndentLevelScope(1))
                using (new EditorGUI.DisabledScope(!apply1))
                {
                    if (pShade1Color != null) me.ColorProperty(pShade1Color, "1st Shading Color");
                    if (pT1 != null) me.ShaderProperty(pT1, "1st Threshold");
                    if (pF1 != null) me.ShaderProperty(pF1, "1st Feather");
                }
            }
            else
            {
                Header("1st Shading");
                if (pShade1Color != null) me.ColorProperty(pShade1Color, "1st Shading Color");
                if (pT1 != null) me.ShaderProperty(pT1, "1st Threshold");
                if (pF1 != null) me.ShaderProperty(pF1, "1st Feather");
            }

            GUILayout.Space(4);

            // 2nd
            if (pApply2 != null)
            {
                bool apply2 = EditorGUILayout.Toggle("Apply to 2nd", GetToggle(pApply2));
                SetToggle(pApply2, apply2);

                using (new EditorGUI.IndentLevelScope(1))
                using (new EditorGUI.DisabledScope(!apply2))
                {
                    if (pShade2Color != null) me.ColorProperty(pShade2Color, "2nd Shading Color");
                    if (pT2 != null) me.ShaderProperty(pT2, "2nd Threshold");
                    if (pF2 != null) me.ShaderProperty(pF2, "2nd Feather");
                }
            }
            else
            {
                Header("2nd Shading");
                if (pShade2Color != null) me.ColorProperty(pShade2Color, "2nd Shading Color");
                if (pT2 != null) me.ShaderProperty(pT2, "2nd Threshold");
                if (pF2 != null) me.ShaderProperty(pF2, "2nd Feather");
            }

            GUILayout.Space(6);

            if (pRampPower != null) me.ShaderProperty(pRampPower, "Ramp Power");
            if (pRampStrength != null) me.ShaderProperty(pRampStrength, "Ramp Strength");
        }

        // Specular
        s_foldSpec = DrawUnityHeader(s_foldSpec, "Specular");
        if (s_foldSpec)
        {
            bool specOn = true;
            if (pApplySpec != null)
            {
                specOn = EditorGUILayout.Toggle("Enable", GetToggle(pApplySpec));
                SetToggle(pApplySpec, specOn);
            }

            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledScope(!specOn))
            {
                if (pSpecColor != null) me.ShaderProperty(pSpecColor, "Spec Color");
                if (pSpecPower != null) me.ShaderProperty(pSpecPower, "Spec Power");
                if (pSpecThreshold != null) me.ShaderProperty(pSpecThreshold, "Spec Threshold");
                if (pSpecFeather != null) me.ShaderProperty(pSpecFeather, "Spec Feather");
            }
        }

        // Rim
        s_foldRim = DrawUnityHeader(s_foldRim, "Rim Light");
        if (s_foldRim)
        {
            if (pRimColor != null) me.ColorProperty(pRimColor, "Rim Color");
            if (pRimPower != null) me.ShaderProperty(pRimPower, "Rim Power");
            if (pRimSoftness != null) me.ShaderProperty(pRimSoftness, "Rim Softness");
            if (pRimWidth != null) me.ShaderProperty(pRimWidth, "Rim Width");
        }

        // Hologram
        s_foldHolo = DrawUnityHeader(s_foldHolo, "Hologram");
        if (s_foldHolo)
        {
            bool holoOn = true;

            if (pHoloEnable != null)
            {
                holoOn = EditorGUILayout.Toggle("Enable", GetToggle(pHoloEnable));
                SetToggle(pHoloEnable, holoOn);
            }

            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledScope(!holoOn))
            {
                if (pHoloColor != null) me.ShaderProperty(pHoloColor, "Holo Color");
                if (pHoloDarkColor != null) me.ShaderProperty(pHoloDarkColor, "Holo Dark Color");

                if (pHoloTiling != null) me.ShaderProperty(pHoloTiling, "Tiling / Meter");
                if (pHoloIntensity != null) me.ShaderProperty(pHoloIntensity, "Intensity");
                if (pHoloDarkIntensity != null) me.ShaderProperty(pHoloDarkIntensity, "Dark Intensity");
                if (pHoloScrollSpeed != null) me.ShaderProperty(pHoloScrollSpeed, "Scroll Speed (World Y)");
                if (pHoloBorderWidth != null) me.ShaderProperty(pHoloBorderWidth, "Border Width");

                if (pHoloSoftness != null) me.ShaderProperty(pHoloSoftness, "Softness");
                if (pHoloBaseY != null) me.ShaderProperty(pHoloBaseY, "Base Y");
                if (pHoloPhaseOffset != null) me.ShaderProperty(pHoloPhaseOffset, "Phase Offset");
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            foreach (var o in me.targets)
                EditorUtility.SetDirty(o);
        }
    }

    private static bool DrawUnityHeader(bool expanded, string title)
    {
        const float h = 22f;

        var rect = EditorGUILayout.GetControlRect(false, h);

        // Background: full row
        var bgRect = rect;
        bgRect.xMin = 0f;
        bgRect.xMax = EditorGUIUtility.currentViewWidth;

        var bg = EditorGUIUtility.isProSkin
            ? new Color(0.18f, 0.18f, 0.18f, 1f)
            : new Color(0.85f, 0.85f, 0.85f, 1f);

        EditorGUI.DrawRect(bgRect, bg);

        // Foldout rect: respect indent
        var foldRect = EditorGUI.IndentedRect(rect);

        var foldStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            fixedHeight = h
        };

        foldRect.y += 1f;

        return EditorGUI.Foldout(foldRect, expanded, title, true, foldStyle);
    }
}
