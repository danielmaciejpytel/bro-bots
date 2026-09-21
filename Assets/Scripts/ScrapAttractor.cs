using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class ScrapAttractor : MonoBehaviour
{
    public float attractorSpeed = 10f;

    private SphereCollider sphereCollider;
    private Rigidbody body;
    private Transform playerTarget;
    private float normalDamping;

    private void Awake()
    {
        sphereCollider = GetComponent<SphereCollider>();
        body = GetComponent<Rigidbody>();
        sphereCollider.enabled = true;
        sphereCollider.isTrigger = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerTarget = other.transform;
            if (body != null)
            {
                normalDamping = body.linearDamping;
                body.linearDamping = 0f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerTarget != null && other.transform == playerTarget)
        {
            playerTarget = null;
            if (body != null)
            {
                body.linearDamping = normalDamping;
            }
        }
    }

    private void FixedUpdate()
    {
        if (playerTarget == null || body == null || body.isKinematic)
        {
            return;
        }

        Vector3 toPlayer = playerTarget.position - body.position;
        float distance = toPlayer.magnitude;
        if (distance <= 0.1f)
        {
            return;
        }

        Vector3 desiredVelocity = toPlayer / distance * attractorSpeed;
        float acceleration = attractorSpeed * 6f;
        body.linearVelocity = Vector3.MoveTowards(
            body.linearVelocity,
            desiredVelocity,
            acceleration * Time.fixedDeltaTime);
    }
}
