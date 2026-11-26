using UnityEngine;
using UnityEngine.InputSystem;

namespace Aetherlands.Coast
{
    internal static class TravelerKeyboard
    {
        public static Vector2 Read()
        {
            var keys = Keyboard.current;
            Vector2 intent = Vector2.zero;
            if (keys == null) return intent;
            if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) intent.x--;
            if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) intent.x++;
            if (keys.sKey.isPressed || keys.downArrowKey.isPressed) intent.y--;
            if (keys.wKey.isPressed || keys.upArrowKey.isPressed) intent.y++;
            return intent;
        }
    }
}
