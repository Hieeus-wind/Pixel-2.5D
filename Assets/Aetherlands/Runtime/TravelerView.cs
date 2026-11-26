using UnityEngine;
namespace Aetherlands.Coast
{
    public sealed class TravelerView : MonoBehaviour
    {
        public SpriteRenderer portrait;
        public Sprite[] frames;
        int direction;
        float stride;
        public void Animate(Vector2 intent,float distance)
        {
            if(intent.sqrMagnitude > 0) direction = Mathf.Abs(intent.x)>Mathf.Abs(intent.y) ? (intent.x>0?2:3) : (intent.y>0?1:0);
            stride = distance > .0001f ? stride+distance : 0;
            int pose = distance > .0001f ? 1+(Mathf.FloorToInt(stride*3)%2) : 0;
            portrait.sprite=frames[direction*3+pose];
        }
    }
}
