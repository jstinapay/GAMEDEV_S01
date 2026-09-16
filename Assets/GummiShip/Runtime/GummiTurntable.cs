using UnityEngine;

namespace GummiShip
{
    /// <summary>
    /// Slow turntable spin for the assignment walkthrough recording.
    /// Attach to the ship root, press Play, orbit the Scene/Game camera.
    /// </summary>
    public class GummiTurntable : MonoBehaviour
    {
        [Tooltip("Degrees per second around the ship's up axis.")]
        public float degreesPerSecond = 20f;

        public bool spin = true;

        void Update()
        {
            if (spin)
                transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
