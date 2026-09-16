using System;

namespace GummiShip
{
    /// <summary>
    /// Logical system a block belongs to. Each category maps to one
    /// parent group under the ship root (Hull, Cockpit, Engines,
    /// Wings, Weapons, Defense, Auxiliary).
    /// </summary>
    public enum GummiCategory
    {
        Hull,
        Cockpit,
        Engine,
        Wing,
        Weapon,
        Defense,
        Auxiliary
    }

    /// <summary>
    /// Overall paint direction. Surveyor is the teal/brass expedition
    /// look; Classic is the chunky red/yellow/blue toy-block look of
    /// classic Kingdom Hearts Gummi Ships.
    /// </summary>
    public enum GummiPaintScheme
    {
        Surveyor,
        Classic
    }

    /// <summary>
    /// Every placeable part in the Gummi Ship builder.
    /// Forward is +Z (sensor nose), up is +Y, port is -X.
    /// </summary>
    public enum GummiBlockType
    {
        HullBlock,
        NoseCone,
        SensorHousing,
        SensorLens,
        Canopy,
        CanopyFrame,
        EngineHousing,
        EngineNozzle,
        WingPanel,
        TipFin,
        RepeaterGun,
        TurretBase,
        TurretBarrel,
        ShieldBlister,
        ShieldProjector,
        CommMast,
        CommDish,
        ScannerPod,
        ScannerLens,
        FuelDrum,
        DrumStrap,
        RCSBlock,
        BeaconRed,
        BeaconGreen,
        LandingSkid
    }

    public static class GummiBlockTypeExtensions
    {
        public static GummiCategory CategoryOf(this GummiBlockType type)
        {
            switch (type)
            {
                case GummiBlockType.HullBlock:
                case GummiBlockType.NoseCone:
                case GummiBlockType.FuelDrum:
                case GummiBlockType.DrumStrap:
                case GummiBlockType.LandingSkid:
                    return GummiCategory.Hull;
                case GummiBlockType.Canopy:
                case GummiBlockType.CanopyFrame:
                    return GummiCategory.Cockpit;
                case GummiBlockType.EngineHousing:
                case GummiBlockType.EngineNozzle:
                    return GummiCategory.Engine;
                case GummiBlockType.WingPanel:
                case GummiBlockType.TipFin:
                    return GummiCategory.Wing;
                case GummiBlockType.RepeaterGun:
                case GummiBlockType.TurretBase:
                case GummiBlockType.TurretBarrel:
                    return GummiCategory.Weapon;
                case GummiBlockType.ShieldBlister:
                case GummiBlockType.ShieldProjector:
                    return GummiCategory.Defense;
                default:
                    return GummiCategory.Auxiliary;
            }
        }

        public static string GroupNameOf(this GummiCategory category)
        {
            switch (category)
            {
                case GummiCategory.Hull: return "Hull";
                case GummiCategory.Cockpit: return "Cockpit";
                case GummiCategory.Engine: return "Engines";
                case GummiCategory.Wing: return "Wings";
                case GummiCategory.Weapon: return "Weapons";
                case GummiCategory.Defense: return "Defense";
                default: return "Auxiliary";
            }
        }

        /// <summary>Swaps Port/Stbd in a label so mirrored blocks stay correctly named.</summary>
        public static string MirrorName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            const string token = "\u0001";
            return name.Replace("Port", token).Replace("Stbd", "Port").Replace(token, "Stbd");
        }
    }
}
