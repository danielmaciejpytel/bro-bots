using UnityEngine;
using UnityEngine.InputSystem;

namespace Cichy.Utility
{
    public static class UtilityClass
    {
        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 vec = GetMouseWorldPositionWithZ(GetPointerScreenPosition(), Camera.main);
            vec.z = 0f;
            return vec;
        }

        public static Vector3 GetMouseWorldPositionWithZ()
        {
            return GetMouseWorldPositionWithZ(GetPointerScreenPosition(), Camera.main);
        }

        public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera)
        {
            return GetMouseWorldPositionWithZ(GetPointerScreenPosition(), worldCamera);
        }

        public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
        {
            if (worldCamera == null)
            {
                return Vector3.zero;
            }

            Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
            return worldPosition;
        }

        private static Vector3 GetPointerScreenPosition()
        {
            if (Pointer.current == null)
            {
                return Vector3.zero;
            }

            Vector2 position = Pointer.current.position.ReadValue();
            return new Vector3(position.x, position.y, 0f);
        }

        public static Vector3 GetDirToMouse(Vector3 fromPosition)
        {
            Vector3 mouseWorldPosition = GetMouseWorldPosition();
            return (mouseWorldPosition - fromPosition).normalized;
        }
    }
}
