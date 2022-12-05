using UnityEngine;

[RequireComponent(typeof(CarController))]
public sealed class CarPlayerController : MonoBehaviour
{
    private CarController _carController;

    #region Standart Unity Functions
    private void Start() => _carController = GetComponent<CarController>();

    private void FixedUpdate()
    {
        _carController.HandleSteering(Input.GetAxis("Horizontal"));

        if (Input.GetKey(KeyCode.W))
            _carController.Accelerate();

        if (Input.GetKeyDown(KeyCode.S))
            _carController.Brake();
    }
    #endregion
}
