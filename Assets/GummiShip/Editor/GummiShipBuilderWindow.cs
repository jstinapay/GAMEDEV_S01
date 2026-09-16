using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Block palette + grid placement tool for building Gummi Ships in
    /// the Scene view. Open via Gummi Ship > Builder Window.
    /// LMB place, RMB (or Erase Mode) remove. Alt+drag still orbits.
    /// </summary>
    public class GummiShipBuilderWindow : EditorWindow
    {
        enum BuildPlane { Top_XZ, Front_XY, Side_ZY }

        GummiShip ship;
        BuildPlane plane = BuildPlane.Top_XZ;
        float level;
        bool mirrorX = true;
        bool placing;
        bool eraseMode;
        GummiBlockType selectedType = GummiBlockType.HullBlock;
        Vector3 brushSize = Vector3.one;
        string fileName = "Vesper_SurveyCutter";
        string status = "Open or create a ship to begin.";
        Vector2 scroll;
        List<string> validationResults;

        bool hoverValid;
        Vector3 hoverCell;
        Vector3 hoverWorld;
        Vector3 hoverSize = Vector3.one;

        static readonly Dictionary<GummiBlockType, Vector3> DefaultSizes =
            new Dictionary<GummiBlockType, Vector3>
            {
                { GummiBlockType.HullBlock, Vector3.one },
                { GummiBlockType.SensorHousing, Vector3.one },
                { GummiBlockType.SensorLens, new Vector3(0.9f, 0.9f, 0.9f) },
                { GummiBlockType.Canopy, new Vector3(1.3f, 0.8f, 2f) },
                { GummiBlockType.CanopyFrame, new Vector3(0.8f, 0.15f, 0.25f) },
                { GummiBlockType.EngineHousing, new Vector3(1.2f, 1.2f, 2.5f) },
                { GummiBlockType.EngineNozzle, new Vector3(1f, 1f, 0.5f) },
                { GummiBlockType.WingPanel, new Vector3(2.5f, 0.25f, 2f) },
                { GummiBlockType.TipFin, new Vector3(0.25f, 1.5f, 1.6f) },
                { GummiBlockType.RepeaterGun, new Vector3(0.4f, 0.4f, 1.8f) },
                { GummiBlockType.TurretBase, new Vector3(0.5f, 0.4f, 0.5f) },
                { GummiBlockType.TurretBarrel, new Vector3(0.25f, 0.25f, 1.4f) },
                { GummiBlockType.ShieldBlister, new Vector3(0.9f, 0.7f, 1.2f) },
                { GummiBlockType.ShieldProjector, new Vector3(0.8f, 0.8f, 0.6f) },
                { GummiBlockType.CommMast, new Vector3(0.3f, 1.6f, 0.3f) },
                { GummiBlockType.CommDish, new Vector3(0.9f, 0.25f, 0.9f) },
                { GummiBlockType.ScannerPod, new Vector3(1.2f, 0.5f, 1.6f) },
                { GummiBlockType.ScannerLens, new Vector3(0.8f, 0.2f, 1f) },
                { GummiBlockType.FuelDrum, new Vector3(1.1f, 1.1f, 2.6f) },
                { GummiBlockType.DrumStrap, new Vector3(1.25f, 1.25f, 0.3f) },
                { GummiBlockType.RCSBlock, new Vector3(0.3f, 0.3f, 0.3f) },
                { GummiBlockType.BeaconRed, new Vector3(0.35f, 0.35f, 0.35f) },
                { GummiBlockType.BeaconGreen, new Vector3(0.35f, 0.35f, 0.35f) },
                { GummiBlockType.LandingSkid, new Vector3(0.3f, 0.3f, 3f) },
            };

        [MenuItem("Gummi Ship/Builder Window")]
        public static void Open()
        {
            GetWindow<GummiShipBuilderWindow>("Gummi Builder");
        }

        [MenuItem("Gummi Ship/New Ship")]
        public static void NewShipMenu()
        {
            var ship = GummiShip.NewShip("GummiShip");
            Selection.activeGameObject = ship.gameObject;
            Open();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Ship", EditorStyles.boldLabel);
            if (ship == null)
            {
                GameObject sel = Selection.activeGameObject;
                if (sel != null)
                    ship = sel.GetComponentInParent<GummiShip>();
            }
            ship = (GummiShip)EditorGUILayout.ObjectField("Active Ship", ship, typeof(GummiShip), true);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Ship"))
            {
                ship = GummiShip.NewShip(string.IsNullOrEmpty(fileName) ? "GummiShip" : fileName);
                Selection.activeGameObject = ship.gameObject;
                status = "Created '" + ship.name + "'.";
            }
            if (GUILayout.Button("Clear Blocks") && ship != null)
            {
                if (EditorUtility.DisplayDialog("Clear Blocks",
                    "Delete every block on '" + ship.name + "'?", "Clear", "Cancel"))
                {
                    ship.ClearBlocks(true);
                    status = "Cleared all blocks.";
                }
            }
            EditorGUILayout.EndHorizontal();
            if (ship != null)
                EditorGUILayout.LabelField("Blocks: " + ship.BlockCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            plane = (BuildPlane)EditorGUILayout.EnumPopup("Grid Plane", plane);
            level = EditorGUILayout.FloatField("Plane Level (cells)", level);
            if (ship != null)
                ship.cellSize = EditorGUILayout.FloatField("Cell Size (m)", Mathf.Max(0.1f, ship.cellSize));
            mirrorX = EditorGUILayout.Toggle("Mirror Port/Starboard", mirrorX);
            eraseMode = EditorGUILayout.Toggle("Erase Mode (LMB removes)", eraseMode);
            placing = EditorGUILayout.Toggle("Placement Active", placing);
            EditorGUILayout.HelpBox(
                "Placement ON: LMB place, RMB remove, Alt+drag orbits.\n" +
                "Blocks snap to the grid plane. Switch planes to build upward.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Block Palette (" + selectedType + ")", EditorStyles.boldLabel);
            GummiCategory lastCategory = (GummiCategory)(-1);
            foreach (GummiBlockType type in Enum.GetValues(typeof(GummiBlockType)))
            {
                GummiCategory category = type.CategoryOf();
                if (category != lastCategory)
                {
                    EditorGUILayout.LabelField(category.GroupNameOf(), EditorStyles.miniBoldLabel);
                    lastCategory = category;
                }
                GUI.backgroundColor = type == selectedType ? Color.cyan : Color.white;
                if (GUILayout.Button(type.ToString()))
                {
                    selectedType = type;
                    Vector3 def;
                    brushSize = DefaultSizes.TryGetValue(type, out def) ? def : Vector3.one;
                }
            }
            GUI.backgroundColor = Color.white;
            brushSize = EditorGUILayout.Vector3Field("Brush Size (cells)", brushSize);
            brushSize.x = Mathf.Max(0.1f, brushSize.x);
            brushSize.y = Mathf.Max(0.1f, brushSize.y);
            brushSize.z = Mathf.Max(0.1f, brushSize.z);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ship File", EditorStyles.boldLabel);
            fileName = EditorGUILayout.TextField("Name", fileName);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save") && ship != null)
            {
                string path = Path.Combine(GummiShipIO.DefaultShipsFolder(), fileName + ".json");
                GummiShipIO.SaveToFile(ship, path);
                AssetDatabase.Refresh();
                status = "Saved " + ship.BlockCount + " blocks to Ships/" + fileName + ".json";
            }
            if (GUILayout.Button("Load") && ship != null)
            {
                string path = Path.Combine(GummiShipIO.DefaultShipsFolder(), fileName + ".json");
                if (!File.Exists(path))
                {
                    status = "No save found at Ships/" + fileName + ".json";
                }
                else
                {
                    int placed = GummiShipIO.LoadFromFile(ship, path, true);
                    status = "Loaded " + placed + " blocks from Ships/" + fileName + ".json";
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Build Vesper Template") && VesperTemplate.BuildWithConfirm(fileName))
                status = "Vesper template built. Run Validate to check it.";
            if (GUILayout.Button("Validate Ship") && ship != null)
                validationResults = GummiShipValidator.Validate(ship);
            if (GUILayout.Button("Add Turntable (for recording)") && ship != null)
            {
                if (ship.GetComponent<GummiTurntable>() == null)
                {
                    Undo.AddComponent<GummiTurntable>(ship.gameObject);
                    status = "Turntable added. Press Play and orbit the camera.";
                }
                else
                {
                    status = "Ship already has a turntable.";
                }
            }

            if (validationResults != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(
                    validationResults.Count == 0 ? "Validation: PASS" : "Validation Issues",
                    EditorStyles.boldLabel);
                if (validationResults.Count == 0)
                    EditorGUILayout.HelpBox("Ship passes all design-rule checks.", MessageType.Info);
                foreach (string issue in validationResults)
                    EditorGUILayout.HelpBox(issue,
                        issue.StartsWith("FAIL") ? MessageType.Error : MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView view)
        {
            if (ship == null || !placing)
            {
                hoverValid = false;
                return;
            }

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            UpdateHover(e);
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
                view.Repaint();

            if (e.type == EventType.MouseDown && !e.alt)
            {
                if (e.button == 0 && !eraseMode && hoverValid)
                {
                    PlaceAt(hoverCell);
                    e.Use();
                }
                else if (e.button == 0 && eraseMode)
                {
                    EraseAt(e);
                    e.Use();
                }
                else if (e.button == 1)
                {
                    EraseAt(e);
                    e.Use();
                }
            }

            if (hoverValid)
            {
                hoverSize = brushSize * ship.cellSize;
                Handles.color = eraseMode ? new Color(1f, 0.3f, 0.3f, 0.25f) : new Color(0.3f, 1f, 1f, 0.25f);
                Handles.DrawWireCube(hoverWorld, hoverSize);
                Handles.Label(hoverWorld + Vector3.up * (hoverSize.y * 0.5f + 0.2f),
                    eraseMode ? "erase" : selectedType.ToString());
            }
        }

        void UpdateHover(Event e)
        {
            hoverValid = false;
            if (ship == null)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 normal = PlaneNormal();
            Vector3 point = PlanePoint();
            var gridPlane = new Plane(normal, point);
            float dist;
            if (!gridPlane.Raycast(ray, out dist))
                return;

            Vector3 world = ray.GetPoint(dist);
            Vector3 local = ship.transform.InverseTransformPoint(world);
            float cell = ship.cellSize;
            switch (plane)
            {
                case BuildPlane.Top_XZ:
                    hoverCell = new Vector3(Mathf.Round(local.x / cell), level, Mathf.Round(local.z / cell));
                    break;
                case BuildPlane.Front_XY:
                    hoverCell = new Vector3(Mathf.Round(local.x / cell), Mathf.Round(local.y / cell), level);
                    break;
                default:
                    hoverCell = new Vector3(level, Mathf.Round(local.y / cell), Mathf.Round(local.z / cell));
                    break;
            }
            hoverWorld = ship.transform.TransformPoint(hoverCell * cell);
            hoverValid = true;
        }

        Vector3 PlaneNormal()
        {
            switch (plane)
            {
                case BuildPlane.Front_XY: return ship.transform.TransformDirection(Vector3.forward);
                case BuildPlane.Side_ZY: return ship.transform.TransformDirection(Vector3.right);
                default: return ship.transform.TransformDirection(Vector3.up);
            }
        }

        Vector3 PlanePoint()
        {
            float cell = ship.cellSize;
            switch (plane)
            {
                case BuildPlane.Front_XY: return ship.transform.TransformPoint(new Vector3(0f, 0f, level * cell));
                case BuildPlane.Side_ZY: return ship.transform.TransformPoint(new Vector3(level * cell, 0f, 0f));
                default: return ship.transform.TransformPoint(new Vector3(0f, level * cell, 0f));
            }
        }

        void PlaceAt(Vector3 cell)
        {
            string name = UniqueName(AutoName(selectedType, cell.x));
            ship.AddBlock(selectedType, cell, brushSize, name, true);
            if (mirrorX && Mathf.Abs(cell.x) > 0.001f)
            {
                var mirrored = new Vector3(-cell.x, cell.y, cell.z);
                ship.AddBlock(selectedType, mirrored, brushSize,
                    UniqueName(GummiBlockTypeExtensions.MirrorName(name)), true);
            }
            status = "Placed " + selectedType + " at " + cell + ".";
            SceneView.RepaintAll();
        }

        void EraseAt(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit))
                return;
            GummiBlock block = hit.collider.GetComponentInParent<GummiBlock>();
            if (block == null)
                return;
            GummiShip owner = block.GetComponentInParent<GummiShip>();
            if (owner != ship)
                return;
            Undo.DestroyObjectImmediate(block.gameObject);
            status = "Removed '" + block.name + "'.";
            SceneView.RepaintAll();
        }

        static string AutoName(GummiBlockType type, float x)
        {
            string side = x < -0.01f ? "Port_" : (x > 0.01f ? "Stbd_" : "");
            switch (type)
            {
                case GummiBlockType.EngineHousing: return side + "Engine";
                case GummiBlockType.EngineNozzle: return side + "EngineNozzle";
                case GummiBlockType.WingPanel: return side + "Wing";
                case GummiBlockType.TipFin: return side + "TipFin";
                case GummiBlockType.RepeaterGun: return side + "Repeater";
                case GummiBlockType.FuelDrum: return side + "FuelDrum";
                case GummiBlockType.TurretBase: return "TailTurret_Base";
                case GummiBlockType.TurretBarrel: return "TailTurret_Barrel";
                case GummiBlockType.SensorLens: return "SensorLantern_Lens";
                case GummiBlockType.SensorHousing: return "SensorLantern_Housing";
                case GummiBlockType.Canopy: return "Canopy";
                case GummiBlockType.CommDish: return "CommDish_OffsetPort";
                case GummiBlockType.CommMast: return "CommMast";
                case GummiBlockType.ScannerPod: return "BellyScannerPod";
                case GummiBlockType.ScannerLens: return "BellyScannerLens";
                case GummiBlockType.ShieldBlister: return side + "ShieldBlister";
                case GummiBlockType.ShieldProjector: return "Aft_ShieldProjector";
                case GummiBlockType.RCSBlock: return side + "RCS";
                case GummiBlockType.BeaconRed: return "Port_NavLight";
                case GummiBlockType.BeaconGreen: return "Stbd_NavLight";
                case GummiBlockType.LandingSkid: return side + "Skid";
                default: return side + type;
            }
        }

        string UniqueName(string desired)
        {
            if (ship == null)
                return desired;
            var taken = new HashSet<string>();
            foreach (GummiBlock block in ship.GetBlocks())
                taken.Add(block.name);
            if (!taken.Contains(desired))
                return desired;
            int i = 2;
            while (taken.Contains(desired + "_" + i))
                i++;
            return desired + "_" + i;
        }
    }
}
