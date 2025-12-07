using UnityEngine;
using UnityEngine.InputSystem;

public enum ButtonState
{
    Default,
    Released,
    Pressed,
    Held,
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance; // Singleton instance

    [SerializeField]
    private int currentDebugPlayerIndex = 1;

    [SerializeField]
    private bool isGrannyControledBySticker = true;

    [Header("Sticker Input Actions")]
    public InputActionReference stickTriggerAction;
    public InputActionReference stickButton2Action;
    public InputActionReference stickButton3Action;
    public InputActionReference stickButton4Action;
    public InputActionReference stickButton5Action;
    public InputActionReference stickButton6Action;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.IsDebug)
        {
            // Switch debug player index with number keys 1-4
            if (Input.GetKeyDown(KeyCode.Alpha1))
                currentDebugPlayerIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha2))
                currentDebugPlayerIndex = 2;
            if (Input.GetKeyDown(KeyCode.Alpha3))
                currentDebugPlayerIndex = 3;
            if (Input.GetKeyDown(KeyCode.Alpha4)) // for Granny
                currentDebugPlayerIndex = 4;
        }
    }

    private void OnEnable()
    {
        EnableInputActions();
    }

    private void OnDisable()
    {
        DisableInputActions();
    }

    public Vector2 GetPlayerMovement(int playerIndex)
    {
        if (GameManager.Instance.IsDebug)
        {
            // In debug mode, allow both keyboard AND gamepad/joystick for the selected debug player.
            // Preference: if a physical gamepad/joystick reports input use it; otherwise fall back to keyboard.
            if (playerIndex == currentDebugPlayerIndex)
            {
                Vector2 keyboardVec = new Vector2(
                    Input.GetAxis("Horizontal"),
                    Input.GetAxis("Vertical")
                );
                Vector2 deviceVec = Vector2.zero;

                switch (playerIndex)
                {
                    case 1:
                    case 2:
                    case 3:
                        deviceVec =
                            Gamepad.all.Count > 0
                                ? Gamepad.all[0].leftStick.ReadValue()
                                : Vector2.zero;
                        break;
                    case 4:
                        if (isGrannyControledBySticker)
                        {
                            deviceVec =
                                Joystick.current != null
                                    ? Joystick.current.stick.ReadValue()
                                    : Vector2.zero;
                        }
                        else
                        {
                            deviceVec =
                                Gamepad.all.Count > 0
                                    ? Gamepad.all[0].leftStick.ReadValue()
                                    : Vector2.zero;
                        }
                        break;
                }

                // Prefer gamepad/joystick input when present; otherwise use keyboard input
                Vector2 vec = deviceVec.sqrMagnitude > 0.0001f ? deviceVec : keyboardVec;
                string vecInfo = vec.sqrMagnitude > 0.0001f ? "Device" : "Keyboard";
                //Debug.Log($"{vecInfo} Input Vector for Player {playerIndex}: {vec}");
                return vec;
            }
            else
            {
                return Vector2.zero;
            }
        }
        else
        {
            switch (playerIndex)
            {
                case 1:
                case 2:
                case 3:
                    return Gamepad.all.Count > (playerIndex - 1)
                        ? Gamepad.all[playerIndex - 1].leftStick.ReadValue()
                        : Vector2.zero;
                case 4:
                    if (isGrannyControledBySticker)
                    {
                        return Joystick.current != null
                            ? Joystick.current.stick.ReadValue()
                            : Vector2.zero;
                    }
                    else
                    {
                        return Gamepad.all.Count > (playerIndex - 1)
                            ? Gamepad.all[playerIndex - 1].leftStick.ReadValue()
                            : Vector2.zero;
                    }
                default:
                    return Vector2.zero;
            }
        }
    }

    public bool GetCapybaraReviveDown(int capybaraIndex)
    {
        if (GameManager.Instance.IsDebug)
        {
            if (capybaraIndex == currentDebugPlayerIndex)
            {
                bool keyboard = Input.GetKeyDown(KeyCode.R);
                bool gamepad =
                    Gamepad.all.Count > 0 && Gamepad.all[0].buttonNorth.wasPressedThisFrame;
                return keyboard || gamepad;
            }
            else
            {
                return false;
            }
        }

        // Regular mode: only gamepad/joystick
        return Gamepad.all.Count > (capybaraIndex - 1)
            && Gamepad.all[capybaraIndex - 1].buttonNorth.wasPressedThisFrame;
    }

    public bool GetCapybaraAbilityDown(int capybaraIndex)
    {
        if (GameManager.Instance.IsDebug)
        {
            if (capybaraIndex == currentDebugPlayerIndex)
            {
                // In debug mode allow keyboard OR gamepad button
                bool keyboard = Input.GetKeyDown(KeyCode.E);
                bool gamepad =
                    Gamepad.all.Count > 0 && Gamepad.all[0].buttonSouth.wasPressedThisFrame;
                return keyboard || gamepad;
            }
            else
            {
                return false;
            }
        }

        // Regular mode: only gamepad/joystick
        return Gamepad.all.Count > (capybaraIndex - 1)
            && Gamepad.all[capybaraIndex - 1].buttonSouth.wasPressedThisFrame;
    }

    public bool GetRightShoulderDown(int playerIndex)
    {
        if (GameManager.Instance.IsDebug)
        {
            if (playerIndex == currentDebugPlayerIndex)
            {
                bool keyboard = Input.GetKeyDown(KeyCode.RightArrow);
                bool gamepad =
                    Gamepad.all.Count > 0 && Gamepad.all[0].rightShoulder.wasPressedThisFrame;
                return keyboard || gamepad;
            }
            else
            {
                return false;
            }
        }

        if (Gamepad.all.Count <= (playerIndex - 1))
            return false;

        return Gamepad.all[playerIndex - 1].rightShoulder.wasPressedThisFrame;
    }

    public bool GetLeftShoulderDown(int playerIndex)
    {
        if (GameManager.Instance.IsDebug)
        {
            if (playerIndex == currentDebugPlayerIndex)
            {
                bool keyboard = Input.GetKeyDown(KeyCode.LeftArrow);
                bool gamepad =
                    Gamepad.all.Count > 0 && Gamepad.all[0].leftShoulder.wasPressedThisFrame;
                return keyboard || gamepad;
            }
            else
            {
                return false;
            }
        }

        if (Gamepad.all.Count <= (playerIndex - 1))
            return false;

        return Gamepad.all[playerIndex - 1].leftShoulder.wasPressedThisFrame;
    }

    public ButtonState GetSouthButtonDownGranny()
    {
        int playerIndex;
        if (GameManager.Instance.IsDebug && currentDebugPlayerIndex == 4)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                return ButtonState.Pressed;
            }
            else if (Input.GetKeyUp(KeyCode.B))
            {
                return ButtonState.Released;
            }
            else if (Input.GetKey(KeyCode.B))
            {
                return ButtonState.Held;
            }
            playerIndex = 0;
        }
        else if (Gamepad.all.Count > 3)
        {
            playerIndex = 3;
        }
        else
        {
            return ButtonState.Default;
        }

        if (
            Gamepad.all.Count > playerIndex
            && Gamepad.all[playerIndex].buttonSouth.wasPressedThisFrame
        )
        {
            return ButtonState.Pressed;
        }
        else if (
            Gamepad.all.Count > playerIndex
            && Gamepad.all[playerIndex].buttonSouth.wasReleasedThisFrame
        )
        {
            return ButtonState.Released;
        }
        else if (Gamepad.all.Count > playerIndex && Gamepad.all[playerIndex].buttonSouth.isPressed)
        {
            return ButtonState.Held;
        }
        return ButtonState.Default;
    }

    public ButtonState GetWestButtonDownGranny()
    {
        int playerIndex;
        if (GameManager.Instance.IsDebug && currentDebugPlayerIndex == 4)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                return ButtonState.Pressed;
            }
            else if (Input.GetKeyUp(KeyCode.N))
            {
                return ButtonState.Released;
            }
            else if (Input.GetKey(KeyCode.N))
            {
                return ButtonState.Held;
            }
            playerIndex = 0;
        }
        else if (Gamepad.all.Count > 3)
        {
            playerIndex = 3;
        }
        else
        {
            return ButtonState.Default;
        }

        if (
            Gamepad.all.Count > playerIndex
            && Gamepad.all[playerIndex].buttonWest.wasPressedThisFrame
        )
        {
            return ButtonState.Pressed;
        }
        else if (
            Gamepad.all.Count > playerIndex
            && Gamepad.all[playerIndex].buttonWest.wasReleasedThisFrame
        )
        {
            return ButtonState.Released;
        }
        else if (Gamepad.all.Count > playerIndex && Gamepad.all[playerIndex].buttonWest.isPressed)
        {
            return ButtonState.Held;
        }
        return ButtonState.Default;
    }

    public ButtonState GetNorthButtonDownGranny()
    {
        int playerIndex;
        if (GameManager.Instance.IsDebug && currentDebugPlayerIndex == 4)
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                return ButtonState.Pressed;
            }
            else if (Input.GetKeyUp(KeyCode.M))
            {
                return ButtonState.Released;
            }
            else if (Input.GetKey(KeyCode.M))
            {
                return ButtonState.Held;
            }
            playerIndex = 0;
        }
        else if (Gamepad.all.Count > 3)
        {
            playerIndex = 3;
        }
        else
        {
            return ButtonState.Default;
        }

        if (
            Gamepad.all.Count > playerIndex
            && Gamepad.all[playerIndex].buttonNorth.wasPressedThisFrame
        )
        {
            return ButtonState.Pressed;
        }
        else if (
            Gamepad.all.Count > playerIndex
            && Gamepad.all[playerIndex].buttonNorth.wasReleasedThisFrame
        )
        {
            return ButtonState.Released;
        }
        else if (Gamepad.all.Count > playerIndex && Gamepad.all[playerIndex].buttonNorth.isPressed)
        {
            return ButtonState.Held;
        }
        return ButtonState.Default;
    }

    #region Sticker Input Actions
    private void EnableInputActions()
    {
        stickTriggerAction?.action?.Enable();
        stickButton2Action?.action?.Enable();
        stickButton3Action?.action?.Enable();
        stickButton4Action?.action?.Enable();
        stickButton5Action?.action?.Enable();
        stickButton6Action?.action?.Enable();
    }

    private void DisableInputActions()
    {
        stickTriggerAction?.action?.Disable();
        stickButton2Action?.action?.Disable();
        stickButton3Action?.action?.Disable();
        stickButton4Action?.action?.Disable();
        stickButton5Action?.action?.Disable();
        stickButton6Action?.action?.Disable();
    }

    public bool GetStickTriggerPressed()
    {
        return stickTriggerAction?.action?.WasPressedThisFrame() ?? false;
    }

    public bool GetStickButton2Pressed()
    {
        return stickButton2Action?.action?.WasPressedThisFrame() ?? false;
    }

    public bool GetStickButton2Holded()
    {
        return stickButton2Action?.action?.IsPressed() ?? false;
    }

    public bool GetStickButton3Pressed()
    {
        return stickButton3Action?.action?.WasPressedThisFrame() ?? false;
    }

    public bool GetStickButton4Pressed()
    {
        return stickButton4Action?.action?.WasPressedThisFrame() ?? false;
    }

    public bool GetStickButton5Pressed()
    {
        return stickButton5Action?.action?.WasPressedThisFrame() ?? false;
    }

    public bool GetStickButton6Pressed()
    {
        return stickButton6Action?.action?.WasPressedThisFrame() ?? false;
    }
    #endregion
}
