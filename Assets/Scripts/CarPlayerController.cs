using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarController))]
public sealed class CarPlayerController : MonoBehaviour
{
    private CarController _carController;
    private PlayerInput _playerInput;

    private void Start()
    {
        _carController = GetComponent<CarController>();

        // Configuring Input System interaction.
        _playerInput = new PlayerInput();
        _playerInput.Enable();
        _playerInput.Car.StartMoving.performed += StartMoving;
        _playerInput.Car.Brake.performed += Brake;
    }

    private void OnDestroy()
    {
        _playerInput.Car.StartMoving.performed -= StartMoving;
        _playerInput.Car.Brake.performed -= Brake;
    }

    // Binding Input System with Car Controller
    private void StartMoving(InputAction.CallbackContext context) => _carController.StartMoving();
    private void Brake(InputAction.CallbackContext context) => _carController.Brake();

    // Steering is better to handle with standart input system
    private void FixedUpdate()
    {
        _carController.HandleSteering(Input.GetAxis("Horizontal"));

        if (Input.GetAxis("Vertical") > 0.0f)
            _carController.Accelerate();
    }
}
