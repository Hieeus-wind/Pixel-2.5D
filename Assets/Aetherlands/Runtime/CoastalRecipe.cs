using UnityEngine;
namespace Aetherlands.Coast
{
    [CreateAssetMenu(menuName="Aetherlands/Coastal recipe")]
    public sealed class CoastalRecipe : ScriptableObject
    {
        public int seed = 7319;
        public Color timber = new Color32(161,108,68,255);
        [Min(1)] public float speed = 3.6f;
        [Min(2)] public float cameraSize = 5.15f;
        public const int Revision = 1;
    }
}
