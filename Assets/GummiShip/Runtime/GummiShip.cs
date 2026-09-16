using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GummiShip
{
    /// <summary>
    /// Root of a built ship. Owns the seven system groups (Hull, Cockpit,
    /// Engines, Wings, Weapons, Defense, Auxiliary) so every block is
    /// always labeled and parented. Ship-local axes: +Z forward (nose),
    /// +Y up, -X port.
    /// </summary>
    [DisallowMultipleComponent]
    public class GummiShip : MonoBehaviour
    {
        [Tooltip("World size of one grid cell in meters.")]
        public float cellSize = 1f;

        readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();

        static readonly GummiCategory[] GroupOrder =
        {
            GummiCategory.Hull,
            GummiCategory.Cockpit,
            GummiCategory.Engine,
            GummiCategory.Wing,
            GummiCategory.Weapon,
            GummiCategory.Defense,
            GummiCategory.Auxiliary
        };

        /// <summary>Creates a new empty ship root with all system groups.</summary>
        public static GummiShip NewShip(string shipName)
        {
            var root = new GameObject(shipName);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(root, "New Gummi Ship");
#endif
            var ship = root.AddComponent<GummiShip>();
            ship.EnsureGroups(true);
            return ship;
        }

        /// <summary>Creates the seven system groups if missing (stable order).</summary>
        public void EnsureGroups(bool recordUndo)
        {
            groups.Clear();
            foreach (GummiCategory category in GroupOrder)
            {
                string groupName = category.GroupNameOf();
                Transform found = transform.Find(groupName);
                if (found == null)
                {
                    var go = new GameObject(groupName);
#if UNITY_EDITOR
                    if (recordUndo)
                        Undo.RegisterCreatedObjectUndo(go, "Create Ship Group");
#endif
                    found = go.transform;
                    found.SetParent(transform, false);
                }
                groups[groupName] = found;
            }
        }

        public Transform GetGroup(GummiCategory category)
        {
            EnsureGroups(false);
            Transform t;
            if (groups.TryGetValue(category.GroupNameOf(), out t))
                return t;
            return transform;
        }

        /// <summary>
        /// Adds one block to the correct system group.
        /// gridCenter/sizeCells are in cells, ship-local. Primitive mesh,
        /// orientation and material are derived from the block type.
        /// </summary>
        public GameObject AddBlock(GummiBlockType type, Vector3 gridCenter,
            Vector3 sizeCells, string label, bool recordUndo)
        {
            PrimitiveType primitive;
            Quaternion rotation;
            ResolveVisual(type, out primitive, out rotation);

            var go = GameObject.CreatePrimitive(primitive);
            go.name = string.IsNullOrEmpty(label) ? type.ToString() : label;
#if UNITY_EDITOR
            if (recordUndo)
                Undo.RegisterCreatedObjectUndo(go, "Place " + type);
#endif
            Transform group = GetGroup(type.CategoryOf());
#if UNITY_EDITOR
            if (recordUndo)
                Undo.SetTransformParent(go.transform, group, "Place " + type);
            else
                go.transform.SetParent(group, false);
#else
            go.transform.SetParent(group, false);
#endif
            go.transform.localPosition = gridCenter * cellSize;
            go.transform.localRotation = rotation;
            go.transform.localScale = sizeCells * cellSize;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = GummiMaterialLibrary.Get(type);

            var meta = go.AddComponent<GummiBlock>();
            meta.blockType = type;
            meta.label = go.name;
            meta.gridCenter = gridCenter;
            meta.sizeCells = sizeCells;
            return go;
        }

        public List<GummiBlock> GetBlocks()
        {
            return new List<GummiBlock>(GetComponentsInChildren<GummiBlock>(true));
        }

        public int BlockCount
        {
            get { return GetComponentsInChildren<GummiBlock>(true).Length; }
        }

        /// <summary>Deletes every block, keeping the empty groups.</summary>
        public void ClearBlocks(bool recordUndo)
        {
            foreach (GummiBlock block in GetBlocks())
            {
#if UNITY_EDITOR
                if (recordUndo)
                    Undo.DestroyObjectImmediate(block.gameObject);
                else
                    DestroyImmediate(block.gameObject);
#else
                Destroy(block.gameObject);
#endif
            }
        }

        static void ResolveVisual(GummiBlockType type, out PrimitiveType primitive, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            switch (type)
            {
                case GummiBlockType.SensorLens:
                case GummiBlockType.Canopy:
                case GummiBlockType.ShieldBlister:
                case GummiBlockType.CommDish:
                case GummiBlockType.BeaconRed:
                case GummiBlockType.BeaconGreen:
                    primitive = PrimitiveType.Sphere;
                    return;
                case GummiBlockType.EngineHousing:
                case GummiBlockType.EngineNozzle:
                case GummiBlockType.FuelDrum:
                case GummiBlockType.DrumStrap:
                    // Cylinders/capsules run along Y by default; ships point +Z.
                    primitive = type == GummiBlockType.FuelDrum ? PrimitiveType.Capsule : PrimitiveType.Cylinder;
                    rotation = Quaternion.Euler(90f, 0f, 0f);
                    return;
                case GummiBlockType.TurretBase:
                case GummiBlockType.CommMast:
                    primitive = PrimitiveType.Cylinder;
                    return;
                default:
                    primitive = PrimitiveType.Cube;
                    return;
            }
        }
    }
}
