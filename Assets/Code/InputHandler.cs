using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour {
    public static Vector2 mousePosition;
    public void ProcessCursorPosition(InputAction.CallbackContext input) => 
        mousePosition = input.ReadValue<Vector2>();
}
