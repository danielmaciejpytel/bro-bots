using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class InputHandler : MonoBehaviour
{
    public Vector2 InputVector { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool DashPressed { get; private set; }

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dashAction;

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction
            .AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction
            .AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Gamepad>/rightTrigger");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        attackAction?.Enable();
        dashAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        attackAction?.Disable();
        dashAction?.Disable();
        InputVector = Vector2.zero;
        AttackPressed = false;
        DashPressed = false;
    }

    private void Update()
    {
        InputVector = moveAction != null
            ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f)
            : Vector2.zero;
        AttackPressed = attackAction != null && attackAction.WasPressedThisFrame();
        DashPressed = dashAction != null && dashAction.WasPressedThisFrame();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        attackAction?.Dispose();
        dashAction?.Dispose();
    }
}
