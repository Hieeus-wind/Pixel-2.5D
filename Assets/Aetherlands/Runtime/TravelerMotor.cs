using UnityEngine;
namespace Aetherlands.Coast
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class TravelerMotor : MonoBehaviour
    {
        public CoastalRecipe recipe;
        public TravelerView view;
        CharacterController capsule;
        void Awake() => capsule = GetComponent<CharacterController>();
        void Update() => Step(TravelerKeyboard.Read(), Time.deltaTime);
        public void Step(Vector2 intent, float seconds)
        {
            intent = Vector2.ClampMagnitude(intent,1);
            var before = transform.position;
            capsule.Move(new Vector3(intent.x * recipe.speed,-3,intent.y * recipe.speed) * seconds);
            Vector3 delta = transform.position-before;
            view.Animate(intent, new Vector2(delta.x,delta.z).magnitude);
        }
    }
}
