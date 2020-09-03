
using UnityEngine;

namespace Fove.Unity
{
    /// <summary>
    /// This component registers and updates the associated game object to the Fove object scene for gaze object detection.
    /// </summary>
    /// <remarks>It requires a Unity collider to extract the object shape</remarks>
    [RequireComponent(typeof(Collider))]
    public partial class GazableObject : MonoBehaviour
    {
        /// <summary>
        /// Specify where the calculation for the object velocity can take place
        /// </summary>
        public enum VelocitySource 
        { 
            /// <summary>
            /// The velocity is calculated from the Unity late update function
            /// </summary>
            LateUpdate, 
            /// <summary>
            /// The velocity is calculated from the Unity fixed update function
            /// </summary>
            FixedUpdate, 
            /// <summary>
            /// The velocity is taken from the Game Object rigidbody
            /// </summary>
            Rigidbody 
        }

        /// <summary>
        /// Specify where the object velocity calculation should be done.
        /// </summary>
        [Tooltip("Specify where the velocity calculation for the object should be done. Select 'Rigidbody' if a rigidbody is associated to this game object, "
            + "'FixedUpdate' if you are changing the object position from the 'FixedUpdate' function, and 'LateUpdate' otherwise.")]
        public VelocitySource velocitySource = VelocitySource.LateUpdate;

        /// <summary>
        /// Create the gazable objects on game object of the given hierarchy having collider components.
        /// </summary>
        /// <param name="hierarchyRoot">The root of the object hierarchy</param>
        public static void CreateFromColliders(GameObject hierarchyRoot)
        {
            CreateFromColliders(hierarchyRoot.GetComponentsInChildren<Collider>());
        }

        /// <summary>
        /// Create the gazable objects on game object of loaded scenes having colliders
        /// </summary>
        /// <remarks>Do not call this function every frames as it is processing the full object hierarchy</remarks>
        public static void CreateFromSceneColliders()
        {
            CreateFromColliders(FindObjectsOfType<Collider>());
        }

        /// <summary>
        /// Rebuild the registered scene object shape.
        /// </summary>
        /// <remarks>Call this function if you changed the game object collider properties at runtime.</remarks>
        public virtual void RebuildShape()
        {
            DestroyGazableObject();
            Initialize();
        }
    }
}
