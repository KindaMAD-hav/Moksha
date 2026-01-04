using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Holographic_Cards.Scripts
{
    public enum Edition
    {
        Regular,
        Polychrome,
        Foil,
        Negative
    }

    /// <summary>
    /// Manages the shader properties of a card's UI image based on its edition and rotation.
    /// </summary>
    public class ShaderManager : MonoBehaviour
    {
        [Header("Edition Settings")]
        [SerializeField] private string[] editionKeywords =
        {
            "REGULAR",
            "POLYCHROME",
            "FOIL",
            "NEGATIVE"
        };

        private static readonly int RotationProperty = Shader.PropertyToID("_Rotation");

        private Image image;
        private Material material;

        [Tooltip("Card edition for shader effect.")]
        public Edition edition;

        private void Start()
        {
            image = GetComponent<Image>();
            // Create a new instance of the material so that changes don't affect shared material
            material = new Material(image.material);
            image.material = material;

            // Disable any previously enabled keywords
            foreach (var keyword in material.enabledKeywords)
            {
                material.DisableKeyword(keyword.name);
            }

            // Enable the shader keyword corresponding to the selected edition
            material.EnableKeyword("_EDITION_" + editionKeywords[(int)edition]);
        }

        private void Update()
        {
            UpdateRotationProperty();
        }

        /// <summary>
        /// Retrieves the parent rotation, clamps and remaps the angles, then updates the shader property.
        /// </summary>
        private void UpdateRotationProperty()
        {
            // Retrieve parent's rotation as Euler angles
            Quaternion currentRotation = transform.parent.localRotation;
            Vector3 eulerAngles = currentRotation.eulerAngles;

            float xAngle = ClampAngle(eulerAngles.x, -90f, 90f);
            float yAngle = ClampAngle(eulerAngles.y, -90f, 90f);

            // Remap the clamped angles from [-20, 20] to [-0.5, 0.5]
            float remappedX = Remap(xAngle, -20f, 20f, -0.5f, 0.5f);
            float remappedY = Remap(yAngle, -20f, 20f, -0.5f, 0.5f);

            material.SetVector(RotationProperty, new Vector2(remappedX, remappedY));
        }

        /// <summary>
        /// Remaps a value from one range to another.
        /// </summary>
        /// <param name="value">The input value.</param>
        /// <param name="from1">Lower bound of the input range.</param>
        /// <param name="to1">Upper bound of the input range.</param>
        /// <param name="from2">Lower bound of the output range.</param>
        /// <param name="to2">Upper bound of the output range.</param>
        /// <returns>The remapped value.</returns>
        private static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        /// <summary>
        /// Clamps an angle between a minimum and maximum value, correctly handling wrap-around.
        /// </summary>
        /// <param name="angle">The angle in degrees.</param>
        /// <param name="min">Minimum allowable angle.</param>
        /// <param name="max">Maximum allowable angle.</param>
        /// <returns>The clamped angle.</returns>
        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -180f)
                angle += 360f;
            if (angle > 180f)
                angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
