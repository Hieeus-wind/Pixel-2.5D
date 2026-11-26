using UnityEngine;
namespace Aetherlands.Coast
{
    public sealed class CoastalCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        void LateUpdate() => transform.position = target.position + offset;
    }
}
