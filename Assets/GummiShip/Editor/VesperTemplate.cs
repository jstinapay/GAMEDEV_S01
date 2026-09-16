using UnityEditor;
using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Generates the Vesper deep-range survey cutter from the design plan:
    /// tapered keel-spine hull, bow sensor lantern, raised canopy, twin
    /// stern engines, stepped-sweep wings with tip fins, wing-root
    /// repeaters, dorsal tail turret, flank fuel drums, comm mast with
    /// port-offset dish, belly scanner, RCS quads and nav beacons.
    /// Ship-local axes: +Z forward, +Y up, -X port.
    /// </summary>
    public static class VesperTemplate
    {
        public const string ShipName = "Vesper_SurveyCutter";

        [MenuItem("Gummi Ship/Build Vesper Template")]
        public static void BuildMenu()
        {
            BuildWithConfirm(ShipName);
        }

        /// <summary>Confirms overwrite, rebuilds the ship, selects it. Returns success.</summary>
        public static bool BuildWithConfirm(string shipName)
        {
            if (string.IsNullOrEmpty(shipName))
                shipName = ShipName;

            GameObject existing = GameObject.Find(shipName);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Rebuild Ship",
                    "'" + shipName + "' already exists. Delete and rebuild?", "Rebuild", "Cancel"))
                    return false;
                Undo.DestroyObjectImmediate(existing);
            }

            GummiShip ship = Build(shipName, true);
            Selection.activeGameObject = ship.gameObject;
            SceneView.FrameLastActiveSceneView();
            EditorUtility.DisplayDialog("Vesper Built",
                "Placed " + ship.BlockCount + " blocks. Run Validate Ship in the Builder Window to check the design rules.",
                "OK");
            return true;
        }

        public static GummiShip Build(string shipName, bool recordUndo)
        {
            var ship = GummiShip.NewShip(shipName);

            // ---- Phase 1: keel-spine hull (bow z+ .. stern z-) ----
            B(ship, GummiBlockType.HullBlock, 0f, 0f, 7f, 1f, 1f, 2f, "KeelSpine_Nose", recordUndo);
            B(ship, GummiBlockType.HullBlock, 0f, 0f, 4.5f, 1f, 1f, 3f, "KeelSpine_Fwd", recordUndo);
            B(ship, GummiBlockType.HullBlock, 0f, 0f, 1f, 3f, 1f, 4f, "KeelSpine_Mid", recordUndo);
            B(ship, GummiBlockType.HullBlock, 0f, 0f, -2.5f, 2f, 1f, 3f, "KeelSpine_Aft", recordUndo);
            B(ship, GummiBlockType.HullBlock, 0f, 0.5f, -6f, 3f, 2f, 1f, "SternTransom", recordUndo);
            // Landing skids on struts (structure you can see).
            B(ship, GummiBlockType.LandingSkid, -1f, -0.95f, 0.5f, 0.3f, 0.3f, 3f, "Port_Skid", recordUndo);
            B(ship, GummiBlockType.LandingSkid, 1f, -0.95f, 0.5f, 0.3f, 0.3f, 3f, "Stbd_Skid", recordUndo);
            B(ship, GummiBlockType.HullBlock, -1f, -0.6f, -0.5f, 0.25f, 0.5f, 0.25f, "Port_SkidStrut_Aft", recordUndo);
            B(ship, GummiBlockType.HullBlock, -1f, -0.6f, 1.5f, 0.25f, 0.5f, 0.25f, "Port_SkidStrut_Fwd", recordUndo);
            B(ship, GummiBlockType.HullBlock, 1f, -0.6f, -0.5f, 0.25f, 0.5f, 0.25f, "Stbd_SkidStrut_Aft", recordUndo);
            B(ship, GummiBlockType.HullBlock, 1f, -0.6f, 1.5f, 0.25f, 0.5f, 0.25f, "Stbd_SkidStrut_Fwd", recordUndo);

            // ---- Phase 2: bow sensor lantern (the ship's signature) ----
            B(ship, GummiBlockType.SensorHousing, 0f, 0f, 8.4f, 1f, 1f, 1f, "SensorLantern_Housing", recordUndo);
            B(ship, GummiBlockType.SensorLens, 0f, 0f, 9.15f, 0.9f, 0.9f, 0.9f, "SensorLantern_Lens", recordUndo);

            // ---- Phase 3: cockpit ----
            B(ship, GummiBlockType.Canopy, 0f, 0.85f, 3.2f, 1.3f, 0.8f, 2f, "Canopy", recordUndo);
            B(ship, GummiBlockType.CanopyFrame, 0f, 1.05f, 2.4f, 0.8f, 0.15f, 0.25f, "CanopyFrame_Fwd", recordUndo);
            B(ship, GummiBlockType.CanopyFrame, 0f, 1.05f, 4f, 0.8f, 0.15f, 0.25f, "CanopyFrame_Aft", recordUndo);

            // ---- Phase 4: twin stern engines ----
            B(ship, GummiBlockType.EngineHousing, -1.6f, 0f, -6.5f, 1.2f, 1.2f, 2.5f, "Port_Engine", recordUndo);
            B(ship, GummiBlockType.EngineHousing, 1.6f, 0f, -6.5f, 1.2f, 1.2f, 2.5f, "Stbd_Engine", recordUndo);
            B(ship, GummiBlockType.EngineNozzle, -1.6f, 0f, -7.9f, 1f, 1f, 0.5f, "Port_EngineNozzle", recordUndo);
            B(ship, GummiBlockType.EngineNozzle, 1.6f, 0f, -7.9f, 1f, 1f, 0.5f, "Stbd_EngineNozzle", recordUndo);

            // ---- Phase 5: stepped-sweep wings + tip fins ----
            B(ship, GummiBlockType.WingPanel, -2.3f, -0.1f, -1.2f, 2.6f, 0.25f, 2.4f, "Port_WingInner", recordUndo);
            B(ship, GummiBlockType.WingPanel, 2.3f, -0.1f, -1.2f, 2.6f, 0.25f, 2.4f, "Stbd_WingInner", recordUndo);
            B(ship, GummiBlockType.WingPanel, -4.4f, -0.1f, -2f, 2.2f, 0.25f, 1.8f, "Port_WingOuter", recordUndo);
            B(ship, GummiBlockType.WingPanel, 4.4f, -0.1f, -2f, 2.2f, 0.25f, 1.8f, "Stbd_WingOuter", recordUndo);
            B(ship, GummiBlockType.TipFin, -5.45f, 0.55f, -2.4f, 0.25f, 1.5f, 1.6f, "Port_TipFin", recordUndo);
            B(ship, GummiBlockType.TipFin, 5.45f, 0.55f, -2.4f, 0.25f, 1.5f, 1.6f, "Stbd_TipFin", recordUndo);

            // ---- Phase 6: weapons (small, secondary on a surveyor) ----
            B(ship, GummiBlockType.RepeaterGun, -1.9f, -0.35f, 0.8f, 0.4f, 0.4f, 1.8f, "Port_Repeater", recordUndo);
            B(ship, GummiBlockType.RepeaterGun, 1.9f, -0.35f, 0.8f, 0.4f, 0.4f, 1.8f, "Stbd_Repeater", recordUndo);
            B(ship, GummiBlockType.TurretBase, 0f, 0.7f, -1.5f, 0.5f, 0.4f, 0.5f, "TailTurret_Base", recordUndo);
            B(ship, GummiBlockType.TurretBarrel, 0f, 0.85f, -2.4f, 0.25f, 0.25f, 1.4f, "TailTurret_Barrel", recordUndo);

            // ---- Phase 7a: defense ----
            B(ship, GummiBlockType.ShieldBlister, -1.9f, 0.15f, -0.5f, 0.9f, 0.7f, 1.2f, "Port_ShieldBlister", recordUndo);
            B(ship, GummiBlockType.ShieldBlister, 1.9f, 0.15f, -0.5f, 0.9f, 0.7f, 1.2f, "Stbd_ShieldBlister", recordUndo);
            B(ship, GummiBlockType.ShieldProjector, 0f, 0.55f, -6.6f, 0.8f, 0.8f, 0.6f, "Aft_ShieldProjector", recordUndo);

            // ---- Phase 7b: auxiliary (the surveyor's real payload) ----
            B(ship, GummiBlockType.CommMast, 0f, 1.3f, 0.5f, 0.3f, 1.6f, 0.3f, "CommMast", recordUndo);
            // Documented asymmetry: dish sits to port to clear the tail turret's arc.
            B(ship, GummiBlockType.CommDish, -0.45f, 2.05f, 0.2f, 0.9f, 0.25f, 0.9f, "CommDish_OffsetPort", recordUndo);
            B(ship, GummiBlockType.ScannerPod, 0f, -0.65f, 1.5f, 1.2f, 0.5f, 1.6f, "BellyScannerPod", recordUndo);
            B(ship, GummiBlockType.ScannerLens, 0f, -0.95f, 1.5f, 0.8f, 0.2f, 1f, "BellyScannerLens", recordUndo);
            B(ship, GummiBlockType.FuelDrum, -1.9f, 0.05f, -3.6f, 1.1f, 2.6f, 1.1f, "Port_FuelDrum", recordUndo);
            B(ship, GummiBlockType.FuelDrum, 1.9f, 0.05f, -3.6f, 1.1f, 2.6f, 1.1f, "Stbd_FuelDrum", recordUndo);
            B(ship, GummiBlockType.DrumStrap, -1.9f, 0.05f, -2.9f, 1.25f, 0.3f, 1.25f, "Port_DrumStrap_Fwd", recordUndo);
            B(ship, GummiBlockType.DrumStrap, -1.9f, 0.05f, -4.3f, 1.25f, 0.3f, 1.25f, "Port_DrumStrap_Aft", recordUndo);
            B(ship, GummiBlockType.DrumStrap, 1.9f, 0.05f, -2.9f, 1.25f, 0.3f, 1.25f, "Stbd_DrumStrap_Fwd", recordUndo);
            B(ship, GummiBlockType.DrumStrap, 1.9f, 0.05f, -4.3f, 1.25f, 0.3f, 1.25f, "Stbd_DrumStrap_Aft", recordUndo);
            // RCS quads: nose cross + wingtip singles.
            B(ship, GummiBlockType.RCSBlock, -0.65f, 0f, 6f, 0.3f, 0.3f, 0.3f, "RCS_Nose_Port", recordUndo);
            B(ship, GummiBlockType.RCSBlock, 0.65f, 0f, 6f, 0.3f, 0.3f, 0.3f, "RCS_Nose_Stbd", recordUndo);
            B(ship, GummiBlockType.RCSBlock, 0f, 0.65f, 6f, 0.3f, 0.3f, 0.3f, "RCS_Nose_Dorsal", recordUndo);
            B(ship, GummiBlockType.RCSBlock, 0f, -0.65f, 6f, 0.3f, 0.3f, 0.3f, "RCS_Nose_Ventral", recordUndo);
            B(ship, GummiBlockType.RCSBlock, -5.45f, 0.5f, -1.5f, 0.3f, 0.3f, 0.3f, "RCS_PortTip", recordUndo);
            B(ship, GummiBlockType.RCSBlock, 5.45f, 0.5f, -1.5f, 0.3f, 0.3f, 0.3f, "RCS_StbdTip", recordUndo);
            // Nav beacons: red port, green starboard, set into the tip fins.
            B(ship, GummiBlockType.BeaconRed, -5.45f, 0f, -3.1f, 0.35f, 0.35f, 0.35f, "Port_NavLight", recordUndo);
            B(ship, GummiBlockType.BeaconGreen, 5.45f, 0f, -3.1f, 0.35f, 0.35f, 0.35f, "Stbd_NavLight", recordUndo);

            return ship;
        }

        static void B(GummiShip ship, GummiBlockType type,
            float x, float y, float z, float sx, float sy, float sz,
            string label, bool recordUndo)
        {
            ship.AddBlock(type, new Vector3(x, y, z), new Vector3(sx, sy, sz), label, recordUndo);
        }
    }
}
