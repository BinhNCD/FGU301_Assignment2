using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerController))]
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerController controller;
    private PlayerInput playerInput;
    private InputActionMap playerActionMap;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        playerActionMap = playerInput.actions.FindActionMap("Player");
    }

    void OnEnable()
    {
        if (playerActionMap != null)
        {
            playerActionMap.Enable();

            InputAction moveAction = playerActionMap.FindAction("Move");
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }

            InputAction jumpAction = playerActionMap.FindAction("Jump");
            if (jumpAction != null)
            {
                jumpAction.performed += OnJump;
            }

            InputAction attackAction = playerActionMap.FindAction("Attack");
            if (attackAction != null)
            {
                attackAction.performed += OnAttack;
            }

            InputAction guardAction = playerActionMap.FindAction("Guard");
            if (guardAction != null)
            {
                guardAction.started += OnGuardStarted;
                guardAction.canceled += OnGuardCanceled;
            }

            InputAction skill1Action = playerActionMap.FindAction("Skill1");
            if (skill1Action != null)
            {
                skill1Action.performed += OnSkill1;
            }

            InputAction skill2Action = playerActionMap.FindAction("Skill2");
            if (skill2Action != null)
            {
                skill2Action.performed += OnSkill2;
            }

            InputAction dashAction = playerActionMap.FindAction("Dash");
            if (dashAction != null)
            {
                dashAction.performed += OnDash;
            }
        }
    }

    void OnDisable()
    {
        if (playerActionMap != null)
        {
            InputAction moveAction = playerActionMap.FindAction("Move");
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }

            InputAction jumpAction = playerActionMap.FindAction("Jump");
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJump;
            }

            InputAction attackAction = playerActionMap.FindAction("Attack");
            if (attackAction != null)
            {
                attackAction.performed -= OnAttack;
            }

            InputAction guardAction = playerActionMap.FindAction("Guard");
            if (guardAction != null)
            {
                guardAction.started -= OnGuardStarted;
                guardAction.canceled -= OnGuardCanceled;
            }

            InputAction skill1Action = playerActionMap.FindAction("Skill1");
            if (skill1Action != null)
            {
                skill1Action.performed -= OnSkill1;
            }

            InputAction skill2Action = playerActionMap.FindAction("Skill2");
            if (skill2Action != null)
            {
                skill2Action.performed -= OnSkill2;
            }

            InputAction dashAction = playerActionMap.FindAction("Dash");
            if (dashAction != null)
            {
                dashAction.performed -= OnDash;
            }

            playerActionMap.Disable();
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (controller == null) return;

        Vector2 moveInput = context.ReadValue<Vector2>();
        controller.SetMoveInput(moveInput);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnJumpPressed();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnAttackPressed();
    }

    public void OnGuardStarted(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnGuardPressed(true);
    }

    public void OnGuardCanceled(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnGuardPressed(false);
    }

    public void OnSkill1(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnSkillPressed(1);
    }

    public void OnSkill2(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnSkillPressed(2);
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (controller == null) return;
        controller.OnDashPressed();
    }
}
