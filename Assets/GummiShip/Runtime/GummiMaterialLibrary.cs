using System.Collections.Generic;
using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Shared materials implementing the Vesper color direction:
    /// teal hull, amber sensor/canopy glass, gunmetal engines with cyan
    /// glow, graphite weapons, brass auxiliary fittings, red/green nav beacons.
    /// Created deterministically at runtime so ships rebuild identically
    /// anywhere without checked-in material assets.
    /// </summary>
    public static class GummiMaterialLibrary
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        public static Material Get(GummiBlockType type)
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

        public static void ClearCache()
        {
            Cache.Clear();
        }

        static Material GetOrCreate(string name, Color baseColor, float metallic,
            float smoothness, bool emissive, Color emissionColor)
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

            Cache[name] = mat;
            return mat;
        }
    }
}
