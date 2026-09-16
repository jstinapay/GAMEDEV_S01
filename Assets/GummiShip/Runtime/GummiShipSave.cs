using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GummiShip
{
    [Serializable]
    public class GummiBlockSave
    {
        public string name;
        public string type;
        public string label;
        public float px, py, pz;
        public float sx = 1f, sy = 1f, sz = 1f;
    }

    [Serializable]
    public class GummiShipSave
    {
        public string shipName;
        public float cellSize = 1f;
        public List<GummiBlockSave> blocks = new List<GummiBlockSave>();
    }

    /// <summary>
    /// JSON save/load for ships. Files live under Assets/GummiShip/Ships
    /// so designs are version-controlled alongside the builder.
    /// </summary>
    public static class GummiShipIO
    {
        public static GummiShipSave Capture(GummiShip ship)
        {
            var save = new GummiShipSave();
            save.shipName = ship.name;
            save.cellSize = ship.cellSize;
            foreach (GummiBlock block in ship.GetBlocks())
            {
                var entry = new GummiBlockSave();
                entry.name = block.name;
                entry.type = block.blockType.ToString();
                entry.label = block.label;
                entry.px = block.gridCenter.x;
                entry.py = block.gridCenter.y;
                entry.pz = block.gridCenter.z;
                entry.sx = block.sizeCells.x;
                entry.sy = block.sizeCells.y;
                entry.sz = block.sizeCells.z;
                save.blocks.Add(entry);
            }
            return save;
        }

        public static void SaveToFile(GummiShip ship, string absolutePath)
        {
            GummiShipSave save = Capture(ship);
            string json = JsonUtility.ToJson(save, true);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, json);
        }

        /// <summary>Rebuilds every block from file into the ship (groups preserved).</summary>
        public static int LoadFromFile(GummiShip ship, string absolutePath, bool recordUndo)
        {
            string json = File.ReadAllText(absolutePath);
            var save = JsonUtility.FromJson<GummiShipSave>(json);
            if (save == null || save.blocks == null)
                return 0;

            ship.cellSize = save.cellSize > 0f ? save.cellSize : 1f;
            ship.ClearBlocks(recordUndo);

            int placed = 0;
            foreach (GummiBlockSave entry in save.blocks)
            {
                GummiBlockType type;
                try
                {
                    type = (GummiBlockType)Enum.Parse(typeof(GummiBlockType), entry.type);
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("[GummiShip] Unknown block type '" + entry.type + "' skipped.");
                    continue;
                }

                ship.AddBlock(type,
                    new Vector3(entry.px, entry.py, entry.pz),
                    new Vector3(entry.sx, entry.sy, entry.sz),
                    string.IsNullOrEmpty(entry.name) ? entry.type : entry.name,
                    recordUndo);
                placed++;
            }
            return placed;
        }

        public static string DefaultShipsFolder()
        {
            return Path.Combine(Application.dataPath, "GummiShip/Ships");
        }
    }
}
