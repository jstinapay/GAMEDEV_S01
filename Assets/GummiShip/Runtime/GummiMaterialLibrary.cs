using System.Collections.Generic;
using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Shared materials for both paint schemes.
    /// Surveyor: teal hull, amber glass, gunmetal + cyan engines,
    /// graphite weapons, brass fittings.
    /// Classic: chunky toy-block Kingdom Hearts look — red/yellow hull
    /// banding, blue glass dome, white wings, orange engines with hot
    /// pink glow, graphite details.
    /// Created deterministically at runtime so ships rebuild identically
    /// anywhere without checked-in material assets.
    /// </summary>
    public static class GummiMaterialLibrary
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Material Get(GummiBlockType type)
        {
            return Get(type, GummiPaintScheme.Surveyor, 0);
        }

        public static Material Get(GummiBlockType type, GummiPaintScheme scheme, int paint)
        {
            if (scheme == GummiPaintScheme.Classic)
                return GetClassic(type, paint);
            return GetSurveyor(type);
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }

        static Material GetSurveyor(GummiBlockType type)
        {
            switch (type)
            {
                case GummiBlockType.SensorLens:
                case GummiBlockType.Canopy:
                case GummiBlockType.ScannerLens:
                    return GetOrCreate("AmberGlow",
                        new Color(0.45f, 0.28f, 0.10f), 0.1f, 0.4f,
                        true, new Color(1.0f, 0.55f, 0.12f) * 2.2f);
                case GummiBlockType.EngineHousing:
                case GummiBlockType.TurretBase:
                    return GetOrCreate("Gunmetal",
                        new Color(0.16f, 0.17f, 0.19f), 0.85f, 0.5f,
                        false, Color.black);
                case GummiBlockType.EngineNozzle:
                    return GetOrCreate("EngineCyan",
                        new Color(0.05f, 0.15f, 0.18f), 0.2f, 0.4f,
                        true, new Color(0.1f, 0.85f, 1.0f) * 2.5f);
                case GummiBlockType.RepeaterGun:
                case GummiBlockType.TurretBarrel:
                case GummiBlockType.RCSBlock:
                case GummiBlockType.LandingSkid:
                    return GetOrCreate("WeaponGraphite",
                        new Color(0.09f, 0.09f, 0.11f), 0.6f, 0.45f,
                        false, Color.black);
                case GummiBlockType.CommMast:
                case GummiBlockType.CommDish:
                case GummiBlockType.ScannerPod:
                case GummiBlockType.DrumStrap:
                case GummiBlockType.ShieldProjector:
                    return GetOrCreate("Brass",
                        new Color(0.45f, 0.30f, 0.13f), 0.9f, 0.45f,
                        false, Color.black);
                case GummiBlockType.BeaconRed:
                    return GetOrCreate("BeaconRed",
                        new Color(0.35f, 0.03f, 0.03f), 0.1f, 0.4f,
                        true, new Color(1.0f, 0.08f, 0.08f) * 2.5f);
                case GummiBlockType.BeaconGreen:
                    return GetOrCreate("BeaconGreen",
                        new Color(0.03f, 0.32f, 0.08f), 0.1f, 0.4f,
                        true, new Color(0.1f, 1.0f, 0.25f) * 2.5f);
                default:
                    return GetOrCreate("HullTeal",
                        new Color(0.10f, 0.23f, 0.26f), 0.55f, 0.5f,
                        false, Color.black);
            }
        }

        static Material GetClassic(GummiBlockType type, int paint)
        {
            switch (type)
            {
                case GummiBlockType.Canopy:
                    return GetOrCreate("ClassicCanopyGlass",
                        new Color(0.25f, 0.55f, 0.95f, 0.55f), 0.1f, 0.7f,
                        true, new Color(0.3f, 0.6f, 1.0f) * 0.8f, true);
                case GummiBlockType.SensorLens:
                case GummiBlockType.ScannerLens:
                    return GetOrCreate("ClassicSensorBlue",
                        new Color(0.10f, 0.30f, 0.80f), 0.1f, 0.5f,
                        true, new Color(0.2f, 0.5f, 1.0f) * 2.5f);
                case GummiBlockType.EngineHousing:
                    return GetOrCreate("ClassicEngineOrange",
                        new Color(0.91f, 0.45f, 0.10f), 0.15f, 0.55f,
                        false, Color.black);
                case GummiBlockType.EngineNozzle:
                    return GetOrCreate("ClassicExhaustPink",
                        new Color(0.45f, 0.05f, 0.30f), 0.1f, 0.4f,
                        true, new Color(1.0f, 0.15f, 0.65f) * 2.5f);
                case GummiBlockType.WingPanel:
                case GummiBlockType.TipFin:
                case GummiBlockType.CanopyFrame:
                    return GetOrCreate("ClassicWhite",
                        new Color(0.93f, 0.93f, 0.95f), 0.05f, 0.6f,
                        false, Color.black);
                case GummiBlockType.RepeaterGun:
                case GummiBlockType.TurretBase:
                case GummiBlockType.TurretBarrel:
                case GummiBlockType.RCSBlock:
                case GummiBlockType.LandingSkid:
                    return GetOrCreate("ClassicGraphite",
                        new Color(0.16f, 0.18f, 0.24f), 0.4f, 0.5f,
                        false, Color.black);
                case GummiBlockType.CommMast:
                case GummiBlockType.CommDish:
                case GummiBlockType.ScannerPod:
                case GummiBlockType.ShieldProjector:
                    return GetOrCreate("ClassicYellow",
                        new Color(0.96f, 0.75f, 0.10f), 0.15f, 0.55f,
                        false, Color.black);
                case GummiBlockType.DrumStrap:
                case GummiBlockType.ShieldBlister:
                    return GetOrCreate("ClassicRed",
                        new Color(0.85f, 0.12f, 0.14f), 0.15f, 0.55f,
                        false, Color.black);
                case GummiBlockType.FuelDrum:
                    return GetOrCreate("ClassicDrumYellow",
                        new Color(0.96f, 0.75f, 0.10f), 0.15f, 0.55f,
                        false, Color.black);
                case GummiBlockType.NoseCone:
                    if (paint == 1)
                        return GetOrCreate("ClassicYellow",
                            new Color(0.96f, 0.75f, 0.10f), 0.15f, 0.55f,
                            false, Color.black);
                    return GetOrCreate("ClassicRed",
                        new Color(0.85f, 0.12f, 0.14f), 0.15f, 0.55f,
                        false, Color.black);
                case GummiBlockType.HullBlock:
                    if (paint == 1)
                        return GetOrCreate("ClassicYellow",
                            new Color(0.96f, 0.75f, 0.10f), 0.15f, 0.55f,
                            false, Color.black);
                    if (paint == 2)
                        return GetOrCreate("ClassicEngineOrange",
                            new Color(0.91f, 0.45f, 0.10f), 0.15f, 0.55f,
                            false, Color.black);
                    return GetOrCreate("ClassicRed",
                        new Color(0.85f, 0.12f, 0.14f), 0.15f, 0.55f,
                        false, Color.black);
                case GummiBlockType.BeaconRed:
                    return GetOrCreate("BeaconRed",
                        new Color(0.35f, 0.03f, 0.03f), 0.1f, 0.4f,
                        true, new Color(1.0f, 0.08f, 0.08f) * 2.5f);
                case GummiBlockType.BeaconGreen:
                    return GetOrCreate("BeaconGreen",
                        new Color(0.03f, 0.32f, 0.08f), 0.1f, 0.4f,
                        true, new Color(0.1f, 1.0f, 0.25f) * 2.5f);
                default:
                    return GetOrCreate("ClassicRed",
                        new Color(0.85f, 0.12f, 0.14f), 0.15f, 0.55f,
                        false, Color.black);
            }
        }

        static Material GetOrCreate(string name, Color baseColor, float metallic,
            float smoothness, bool emissive, Color emissionColor)
        {
            return GetOrCreate(name, baseColor, metallic, smoothness, emissive, emissionColor, false);
        }

        static Material GetOrCreate(string name, Color baseColor, float metallic,
            float smoothness, bool emissive, Color emissionColor, bool transparent)
        {
            Material mat;
            if (Cache.TryGetValue(name, out mat) && mat != null)
                return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            bool isUrp = shader != null;
            if (!isUrp)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.name = "Gummi_" + name;
            if (isUrp)
            {
                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
            }
            else
            {
                mat.SetColor("_Color", baseColor);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Glossiness", smoothness);
            }

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_AlphaClip", 0f);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }

            Cache[name] = mat;
            return mat;
        }
    }
}
