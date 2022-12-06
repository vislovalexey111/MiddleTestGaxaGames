using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarController))]
public sealed class CarPlayerInput : MonoBehaviour
{
    private CarController _carController;
    private PlayerInput _playerInput;

    #region Standart Unity Functions
    private void Start()
    {
        _carController = GetComponent<CarController>();

        // Initializing Input System asset and binding it with methods
        _playerInput = new();
        _playerInput.Enable();
        _playerInput.Car.Move.performed += Move;
        _playerInput.Car.Accelerate.performed += Accelerate;
        _playerInput.Car.Accelerate.canceled += StopAccelerating;
        _playerInput.Car.StopMovingBack.performed += StopMovingBack;
    }

    private void OnDestroy()
    {
        _playerInput.Car.Move.performed -= Move;
        _playerInput.Car.Accelerate.performed -= Accelerate;
        _playerInput.Car.Accelerate.canceled -= StopAccelerating;
        _playerInput.Car.StopMovingBack.performed -= StopMovingBack;
        _playerInput.Disable();
    }

    // Continuous input better to handle with standart input manager
    private void FixedUpdate()
    {
        _carController.HandleSteering(Input.GetAxis("Horizontal"));

        if (Input.GetKey(KeyCode.S))
            _carController.BrakeOrMoveBack();
    }
    #endregion

    #region Binding Methods
    private void Move(InputAction.CallbackContext _) => _carController.Move();
    private void Accelerate(InputAction.CallbackContext _) => _carController.Accelerate();
    private void StopAccelerating(InputAction.CallbackContext _) => _carController.StopAccelerating();
    private void StopMovingBack(InputAction.CallbackContext _) => _carController.StopMovingBack();
    #endregion
}
