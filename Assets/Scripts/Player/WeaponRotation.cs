using UnityEngine;

public class WeaponRotation : MonoBehaviour
{
    public float rotationAnglePerSecond = 15f;
    public bool startRotating = false;
    public bool resetRotation = true;

    private void Update()
    {
        if (!startRotating)
        {
            return;
        }

        // Existing prefab values were tuned per frame around 60 FPS.
        // Multiplying by 60 preserves that feel while making rotation frame-rate independent.
        transform.Rotate(0f, rotationAnglePerSecond * 60f * Time.deltaTime, 0f);
    }
}
