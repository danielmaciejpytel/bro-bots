using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Scrap : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float deceleration = 5f;
    [SerializeField] private int scrapValue = 1;

    private ScrapManager scrapManager;
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        scrapManager = ScrapManager.Instance != null
            ? ScrapManager.Instance
            : Object.FindFirstObjectByType<ScrapManager>();

        if (body != null && !body.isKinematic)
        {
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.linearDamping = Mathf.Max(body.linearDamping, deceleration);

            Vector2 horizontal = Random.insideUnitCircle;
            Vector3 launchVelocity = new Vector3(
                horizontal.x * moveSpeed,
                Random.Range(moveSpeed * 0.35f, moveSpeed * 0.8f),
                horizontal.y * moveSpeed);

            body.linearVelocity += launchVelocity;
            body.angularVelocity = Random.insideUnitSphere * moveSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (scrapManager != null && !scrapManager.IsPlayerDead)
            {
                scrapManager.AddScraps(scrapValue);
            }

            Destroy(gameObject);
        }
    }
}
