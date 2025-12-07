using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public enum ControllerType
{
    Xbox,
    PlayStation,
    SwitchPro,
    GenericGamepad,
    KeyboardMouse,
}

public static class ControllerHelper
{
    public static ControllerType GetDeviceType(InputDevice device)
    {
        if (device == null)
        {
            return ControllerType.KeyboardMouse;
        }

        string deviceLayout = device.layout.ToLower();
        Debug.Log($"Device Layout: {deviceLayout}");

        if (deviceLayout.Contains("xinput") || deviceLayout.Contains("xbox"))
        {
            return ControllerType.Xbox;
        }
        else if (
            deviceLayout.Contains("dualshock")
            || deviceLayout.Contains("dualsense")
            || deviceLayout.Contains("ps4")
            || deviceLayout.Contains("ps5")
        )
        {
            return ControllerType.PlayStation;
        }
        else if (deviceLayout.Contains("switchpro") || deviceLayout.Contains("joycon"))
        {
            return ControllerType.SwitchPro;
        }
        else if (device is Gamepad)
        {
            return ControllerType.GenericGamepad;
        }
        else
        {
            return ControllerType.KeyboardMouse;
        }
    }
}
