using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class ProximityDropGate : MonoBehaviour
{
    [Header("Opening")]
    [SerializeField]
    [Tooltip("How close the player must get to the gate collider before it starts opening.")]
    private float triggerDistance = 2.5f;

    [SerializeField]
    [Tooltip("Final local Y position of the gate after opening.")]
    private float targetLocalY = -3.499f;

    [SerializeField]
    [Tooltip("Gate movement speed in local units per second.")]
    private float openSpeed = 5f;

    [Header("State")]
    [SerializeField, Tooltip("Useful for testing the gate already opened.")]
    private bool startOpen;

    private Collider gateCollider;
    private Transform player;
    private bool opening;
    private bool opened;

    public bool IsOpen => opened;

    private void Awake()
    {
        gateCollider = GetComponent<Collider>();
        ResolvePlayer();

        if (startOpen)
        {
            Vector3 position = transform.localPosition;
            position.y = targetLocalY;
            transform.localPosition = position;
            opened = true;
        }
    }

    private void Update()
    {
        if (opened)
        {
            return;
        }

        if (!opening)
        {
            if (player == null)
            {
                ResolvePlayer();
                if (player == null)
                {
                    return;
                }
            }

            Vector3 closestPoint = gateCollider != null
                ? gateCollider.ClosestPoint(player.position)
                : transform.position;

            float distanceSqr = (player.position - closestPoint).sqrMagnitude;
            if (distanceSqr <= triggerDistance * triggerDistance)
            {
                opening = true;
            }
        }

        if (opening)
        {
            Vector3 position = transform.localPosition;
            position.y = Mathf.MoveTowards(
                position.y,
                targetLocalY,
                openSpeed * Time.deltaTime);
            transform.localPosition = position;

            if (Mathf.Approximately(position.y, targetLocalY))
            {
                opened = true;
                opening = false;
            }
        }
    }

    private void ResolvePlayer()
    {
        PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
        player = controller != null ? controller.transform : null;
    }
}
