using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public enum State
{
    WeaponStage1,
    WeaponStage2,
    WeaponStage3,
    WeaponStage4,
    Dash,
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : MonoBehaviour
{
    [Header("Debug / Testing")]
    [SerializeField]
    [Tooltip("Player cannot die while enabled. Useful for testing scenes and hazards.")]
    private bool godMode;

    public bool IsGodMode => godMode;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    [FormerlySerializedAs("activeRollSpeed")]
    public float activeDashSpeed = 25f;
    public float dashAmount = 5f;
    public Transform cameraTransform;
    [SerializeField, HideInInspector, FormerlySerializedAs("cam")]
    private Camera legacyMovementCamera;

    [Header("Dash Cooldown")]
    public float dashCooldown = 3f;
    private float dashCooldownTimer = 0f;
    private bool canDash = true;

    [Header("Weapon States Stats")]
    [SerializeField] private GameObject handhead; // Umożliwia przypisanie w Inspektorze
    [SerializeField] private RuntimeAnimatorController handheadController;
    public float firstWeaponAOERadius = 3.75f;
    public float secondWeaponAOERadius = 4.25f;
    public float thirdWeaponAOERadius = 4.5f;
    public float fourthWeaponAOERadius = 5f;

    public int secondWeaponStateScrap = 70;
    public int thirdWeaponStateScrap = 140;
    public int fourthWeaponStateScrap = 210;

    [Header("AoE Cooldown")]
    public bool aoeReady;
    public float aoeCooldown = 2f;
    private float aoeCooldownCurrent = 3f;
    public Slider aoeSlider;

    [Header(("Push Power"))]
    public float firstWeaponPushPower;
    public float secondWeaponPushPower;
    public float thirdWeaponPushPower;
    public float fourthWeaponPushPower;

    public ScrapManager scrapManager;
    public Animator characterAnim; // Animator postaci
    public Animator handheadAnim;   // Animator broni
    private Rigidbody _rb;
    private Vector3 _moveDir;
    private Vector3 _dashDir;
    private float _dashSpeed;
    private bool _attackInProgress;
    private State _state;
    private InputHandler _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<InputHandler>();
        characterAnim = GetComponent<Animator>();
        if (characterAnim == null || characterAnim.runtimeAnimatorController == null)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    characterAnim = animator;
                    break;
                }
            }
        }

        if (characterAnim != null)
        {
            characterAnim.applyRootMotion = false;
        }

        if (handhead == null)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t.name == "handhead")
                {
                    handhead = t.gameObject;
                    break;
                }
            }
        }

        // Zainicjalizuj animator broni
        if (handhead != null)
        {
            handheadAnim = handhead.GetComponent<Animator>();
            if (handheadAnim == null)
            {
                handheadAnim = handhead.GetComponentInChildren<Animator>(true);
            }

            if (handheadAnim != null &&
                handheadAnim.runtimeAnimatorController == null &&
                handheadController != null)
            {
                handheadAnim.runtimeAnimatorController = handheadController;
            }

            if (handheadAnim == null)
            {
                Debug.LogWarning("Animator broni (handhead) nie został znaleziony!");
            }
        }
        else
        {
            Debug.LogWarning("Obiekt 'handhead' nie został przypisany w Inspektorze!");
        }

        ResolveCameraReference();

        if (scrapManager == null)
        {
            scrapManager = UnityEngine.Object.FindFirstObjectByType<ScrapManager>();
        }
    }

    private void ResolveCameraReference()
    {
        if (cameraTransform != null)
        {
            return;
        }

        if (legacyMovementCamera != null)
        {
            cameraTransform = legacyMovementCamera.transform;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
    }

    private void OnDisable()
    {
        if (characterAnim != null)
        {
            characterAnim.SetBool("isRunning", false);
        }
    }

    public void Die()
    {
        if (godMode)
        {
            return;
        }

        if (characterAnim != null)
        {
            characterAnim.SetBool("isRunning", false);
            characterAnim.enabled = false;
        }

        if (handheadAnim != null)
        {
            handheadAnim.enabled = false;
        }

        if (_input != null)
        {
            _input.enabled = false;
        }

        if (_rb != null)
        {
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            _rb.isKinematic = true;
        }

        var scrapExp = GetComponent<ScrapExplosion>();
        if (scrapExp != null)
        {
            scrapExp.DropScrap(true);
        }

        // Hide renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = false;
        }

        // Disable colliders
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders)
        {
            c.enabled = false;
        }

        // Disable UI canvases on player
        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            canvas.gameObject.SetActive(false);
        }

        enabled = false;
    }

    private void Update()
    {
        UpdateDashCooldown();

        switch (_state)
        {
            case State.WeaponStage1:
                PlayerMovement();
                AoeAttackCooldown();
                PlayerDash();
                break;
            case State.WeaponStage2:
                PlayerMovement();
                AoeAttackCooldown();
                PlayerDash();
                break;
            case State.WeaponStage3:
                PlayerMovement();
                AoeAttackCooldown();
                PlayerDash();
                break;
            case State.WeaponStage4:
                PlayerMovement();
                AoeAttackCooldown();
                PlayerDash();
                break;

            case State.Dash:
                DashMovement();
                break;
        }
        /*
        if (scrapManager.scrapNumber <= 69)
        {
            _state = State.WeaponStage1;
        }
        if (scrapManager.scrapNumber >= secondWeaponStateScrap)
        {
            _state = State.WeaponStage2;
        }

        if (scrapManager.scrapNumber >= thirdWeaponStateScrap)
        {
            _state = State.WeaponStage3;
        }
        if (scrapManager.scrapNumber >= fourthWeaponStateScrap)
        {
            _state = State.WeaponStage4;
        }
        */
    }

    private void AoeAttackCooldown()
    {
        if (aoeCooldown <= 0f)
        {
            aoeCooldownCurrent = 0f;
            aoeReady = true;
        }
        else if (aoeCooldownCurrent >= aoeCooldown)
        {
            aoeReady = true;
        }
        else
        {
            aoeCooldownCurrent += Time.deltaTime;
            aoeCooldownCurrent = Mathf.Clamp(aoeCooldownCurrent, 0.0f, aoeCooldown);
            aoeReady = false;
        }

        if (aoeSlider != null)
        {
            aoeSlider.value = aoeCooldown <= 0f ? 1f : aoeCooldownCurrent / aoeCooldown;

            if (aoeSlider.value >= 1.0f)
            {
                aoeSlider.gameObject.SetActive(false);
            }
            else
            {
                aoeSlider.gameObject.SetActive(true);
            }
        }

        if (_input != null && _input.AttackPressed && aoeReady)
        {
            AttackAoe();
            if (CanPlayAnimation(characterAnim))
            {
                characterAnim.SetTrigger("Attack_WeaponStage1"); // Animator postaci
            }
            if (CanPlayAnimation(handheadAnim))
            {
                handheadAnim.SetTrigger("Hand_Throw"); // Animator broni
            }
            aoeCooldownCurrent = 0.0f;
        }
    }

    private void PlayerMovement()
    {
        ResolveCameraReference();

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();
        else forward = Vector3.forward;

        Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        right.y = 0;
        if (right.sqrMagnitude > 0.001f) right.Normalize();
        else right = Vector3.right;

        Vector2 inputVec = _input != null ? _input.InputVector : Vector2.zero;
        Vector3 movementVector = Vector3.ClampMagnitude(
            forward * inputVec.y + right * inputVec.x,
            1f);

        _moveDir = movementVector;

        if (characterAnim != null)
        {
            characterAnim.SetBool("isRunning", movementVector.sqrMagnitude > 0.001f);
        }
    }

    #region Attacking

    public void AttackAoe()
    {
        if (_attackInProgress)
        {
            return;
        }

        _attackInProgress = true;
        StartCoroutine(AttackSequenceAOE(GetCurrentAttackRadius()));
    }

    private IEnumerator AttackSequenceAOE(float attackRadius)
    {
        yield return new WaitForSeconds(0.2f);
        CheckForEnemiesAndDealAoeDamage(attackRadius);
        yield return new WaitForSeconds(0.5f);
        _attackInProgress = false;
    }

    private void CheckForEnemiesAndDealAoeDamage(float attackRadius)
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);
        HashSet<EnemyMovement> pushedEnemies = new HashSet<EnemyMovement>();

        foreach (Collider c in colliders)
        {
            EnemyMovement enemy = c.GetComponentInParent<EnemyMovement>();
            if (enemy != null && pushedEnemies.Add(enemy))
            {
                enemy.Push();
            }
        }
    }

    private float GetCurrentAttackRadius()
    {
        switch (_state)
        {
            case State.WeaponStage2:
                return secondWeaponAOERadius;
            case State.WeaponStage3:
                return thirdWeaponAOERadius;
            case State.WeaponStage4:
                return fourthWeaponAOERadius;
            default:
                return firstWeaponAOERadius;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, firstWeaponAOERadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, secondWeaponAOERadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, thirdWeaponAOERadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, fourthWeaponAOERadius);
    }

    #endregion

    #region Dash Movement

    private void DashMovement()
    {
        float dashSpeedDropMultiplier = 5f;
        _dashSpeed -= _dashSpeed * dashSpeedDropMultiplier * Time.deltaTime;

        float dashSpeedMinimum = Mathf.Max(1f, activeDashSpeed * 0.1f);
        if (_dashSpeed < dashSpeedMinimum)
        {
            _dashSpeed = 0f;
            _state = State.WeaponStage1;
        }
    }

    private void PlayerDash()
    {
        if (canDash && _input != null && _input.DashPressed)
        {
            _dashDir = _moveDir.sqrMagnitude > 0.001f ? _moveDir.normalized : transform.forward;
            _dashSpeed = activeDashSpeed;
            _state = State.Dash;

            if (CanPlayAnimation(characterAnim))
            {
                characterAnim.SetTrigger("Dash");
            }

            canDash = false;
            dashCooldownTimer = dashCooldown;
        }
    }

    private void UpdateDashCooldown()
    {
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if (_rb == null || _rb.isKinematic)
        {
            return;
        }

        Vector3 planarVelocity = _state == State.Dash
            ? _dashDir * _dashSpeed
            : _moveDir * moveSpeed;

        Vector3 velocity = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(planarVelocity.x, velocity.y, planarVelocity.z);

        Vector3 facingDirection = _state == State.Dash ? _dashDir : _moveDir;
        if (facingDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection);
            float degrees = rotateSpeed * 60f * Time.fixedDeltaTime;
            Quaternion nextRotation = Quaternion.RotateTowards(_rb.rotation, targetRotation, degrees);
            _rb.MoveRotation(nextRotation);
        }
    }

    private static bool CanPlayAnimation(Animator animator)
    {
        return animator != null &&
               animator.isActiveAndEnabled &&
               animator.runtimeAnimatorController != null;
    }

    #endregion
}

