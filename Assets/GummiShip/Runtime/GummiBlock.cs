using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Metadata attached to every placed block. Position/size are in
    /// grid cells relative to the ship root (forward = +Z).
    /// </summary>
    [DisallowMultipleComponent]
    public class GummiBlock : MonoBehaviour
    {
        public GummiBlockType blockType = GummiBlockType.HullBlock;

        [Tooltip("Display label, e.g. Port_Engine. Mirrored placements swap Port/Stbd automatically.")]
        public string label = "Block";

        [Tooltip("Center of the block in grid cells, ship-local.")]
        public Vector3 gridCenter = Vector3.zero;

        [Tooltip("Paint slot: 0 = primary, 1 = secondary, 2 = accent. Used by the Classic scheme for hull banding.")]
        public int paintIndex;

        [Tooltip("Size of the block in grid cells (allows fractional detail parts).")]
        public Vector3 sizeCells = Vector3.one;

        public GummiCategory Category
        {
            get { return blockType.CategoryOf(); }
        }
    }
}
