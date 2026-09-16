using System.Collections.Generic;
using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Design-rule checks: structural connectivity (no floating parts),
    /// port/starboard symmetry, engine/sensor placement sanity and the
    /// minimum systems a functional ship needs. Used by the Builder
    /// window's Validate button and the assignment's final checklist.
    /// </summary>
    public static class GummiShipValidator
    {
        const float Tolerance = 0.06f;

        public static List<string> Validate(GummiShip ship)
        {
            var issues = new List<string>();
            if (ship == null)
            {
                issues.Add("FAIL: No ship assigned.");
                return issues;
            }

            List<GummiBlock> blocks = ship.GetBlocks();
            if (blocks.Count == 0)
            {
                issues.Add("FAIL: Ship has no blocks.");
                return issues;
            }

            CheckConnectivity(ship, blocks, issues);
            CheckSymmetry(ship, blocks, issues);
            CheckLayout(ship, blocks, issues);
            CheckRequiredSystems(blocks, issues);
            return issues;
        }

        static void CheckConnectivity(GummiShip ship, List<GummiBlock> blocks, List<string> issues)
        {
            var bounds = new List<Bounds>(blocks.Count);
            foreach (GummiBlock block in blocks)
            {
                Collider collider = block.GetComponent<Collider>();
                bounds.Add(collider != null ? collider.bounds : DefaultBounds(ship, block));
            }

            // Start from the block nearest the ship origin (the keel core).
            int start = 0;
            float best = float.MaxValue;
            for (int i = 0; i < blocks.Count; i++)
            {
                float d = (bounds[i].center - ship.transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    start = i;
                }
            }

            var reached = new HashSet<int>();
            var frontier = new Queue<int>();
            reached.Add(start);
            frontier.Enqueue(start);
            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                Bounds grown = bounds[current];
                grown.Expand(Tolerance);
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (!reached.Contains(i) && grown.Intersects(bounds[i]))
                    {
                        reached.Add(i);
                        frontier.Enqueue(i);
                    }
                }
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                if (!reached.Contains(i))
                    issues.Add("FAIL: '" + blocks[i].name + "' is floating (not connected to the hull).");
            }
        }

        static void CheckSymmetry(GummiShip ship, List<GummiBlock> blocks, List<string> issues)
        {
            foreach (GummiBlock block in blocks)
            {
                Vector3 rel = ship.transform.InverseTransformPoint(BlockCenter(ship, block));
                if (Mathf.Abs(rel.x) < 0.01f)
                    continue; // centerline parts are symmetric by definition

                bool found = false;
                foreach (GummiBlock other in blocks)
                {
                    if (other == block || other.blockType != block.blockType)
                        continue;
                    Vector3 otherRel = ship.transform.InverseTransformPoint(BlockCenter(ship, other));
                    if (Mathf.Abs(otherRel.x + rel.x) < 0.15f
                        && Mathf.Abs(otherRel.y - rel.y) < 0.15f
                        && Mathf.Abs(otherRel.z - rel.z) < 0.15f)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    issues.Add("WARN: '" + block.name + "' has no mirrored counterpart (intentional asymmetry should be documented).");
            }
        }

        static void CheckLayout(GummiShip ship, List<GummiBlock> blocks, List<string> issues)
        {
            float sumZ = 0f;
            float engineZ = 0f;
            int engineCount = 0;
            float maxZ = float.MinValue;
            string noseName = null;

            foreach (GummiBlock block in blocks)
            {
                Vector3 rel = ship.transform.InverseTransformPoint(BlockCenter(ship, block));
                sumZ += rel.z;
                if (rel.z > maxZ)
                {
                    maxZ = rel.z;
                    noseName = block.name;
                }
                if (block.blockType == GummiBlockType.EngineHousing ||
                    block.blockType == GummiBlockType.EngineNozzle)
                {
                    engineZ += rel.z;
                    engineCount++;
                }
            }

            float midZ = sumZ / blocks.Count;
            if (engineCount > 0 && engineZ / engineCount > midZ)
                issues.Add("FAIL: Engines sit ahead of the ship's mid-point; thrust would push through the bow.");

            bool noseIsSensor = noseName != null &&
                (noseName.Contains("Sensor") || noseName.Contains("Lantern") || noseName.Contains("Nose"));
            if (!noseIsSensor && noseName != null)
                issues.Add("WARN: Forward-most block is '" + noseName + "' (expected the sensor lantern at the bow).");
        }

        static void CheckRequiredSystems(List<GummiBlock> blocks, List<string> issues)
        {
            bool engine = false, canopy = false, sensor = false, weapon = false;
            foreach (GummiBlock block in blocks)
            {
                switch (block.blockType)
                {
                    case GummiBlockType.EngineHousing: engine = true; break;
                    case GummiBlockType.Canopy: canopy = true; break;
                    case GummiBlockType.SensorLens: sensor = true; break;
                    case GummiBlockType.RepeaterGun:
                    case GummiBlockType.TurretBase:
                    case GummiBlockType.TurretBarrel:
                        weapon = true;
                        break;
                }
            }
            if (!engine) issues.Add("FAIL: No engine block; the ship cannot move.");
            if (!canopy) issues.Add("FAIL: No cockpit canopy; the ship has no command section.");
            if (!sensor) issues.Add("WARN: No sensor lens; a surveyor without its lantern eye is just a hull.");
            if (!weapon) issues.Add("WARN: No weapon blocks; the ship is unarmed.");
        }

        static Vector3 BlockCenter(GummiShip ship, GummiBlock block)
        {
            Collider collider = block.GetComponent<Collider>();
            if (collider != null)
                return collider.bounds.center;
            return ship.transform.TransformPoint(block.gridCenter * ship.cellSize);
        }

        static Bounds DefaultBounds(GummiShip ship, GummiBlock block)
        {
            Vector3 center = ship.transform.TransformPoint(block.gridCenter * ship.cellSize);
            return new Bounds(center, block.sizeCells * ship.cellSize);
        }
    }
}
