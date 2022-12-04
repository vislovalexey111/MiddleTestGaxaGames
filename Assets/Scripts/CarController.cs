using System.Collections;
using UnityEngine;

[System.Serializable]
public sealed class WheelInfo
{
    public WheelCollider Collider;
    public Transform Mesh;
}

[RequireComponent(typeof(Rigidbody))]
public sealed class CarController : MonoBehaviour
{
    [Header("Wheels")]
    [SerializeField] private WheelInfo _wheelFrontLeft;
    [SerializeField] private WheelInfo _wheelFrontRight;
    [SerializeField] private WheelInfo _wheelRearLeft;
    [SerializeField] private WheelInfo _wheelRearRight;

    [Header("Parameters")]
    [SerializeField] private float _maxSteeringAngle;    
    [SerializeField] private float _wheelTorque;
    [SerializeField] private float _moveBackSpeed;
    [SerializeField] private float _moveSpeed;

    private Rigidbody _rigidbody;
    private Coroutine _moveCoroutine;
    private Coroutine _breakCoroutine;
    private bool _started;

    private void Start()
    {
        _started = false;
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out SpeedPlatform speedPlatform))
            _rigidbody.velocity += _rigidbody.velocity.magnitude * speedPlatform.SpeedScaler * other.transform.forward;
    }

    private void FixedUpdate() => UpdateWheelVisuals();

    public void HandleSteering(float angle)
    {
        angle *= _maxSteeringAngle;

        _wheelFrontRight.Collider.steerAngle = angle;
        _wheelFrontLeft.Collider.steerAngle = angle;
    }

    private void UpdateWheelVisuals()
    {   
        UpdateWheelVisual(_wheelRearRight);
        UpdateWheelVisual(_wheelRearLeft);
        UpdateWheelVisual(_wheelFrontRight);
        UpdateWheelVisual(_wheelFrontLeft);
    }

    private void UpdateWheelVisual(WheelInfo wheel)
    {
        wheel.Collider.GetWorldPose(out Vector3 wheelPosition, out Quaternion wheelRotation);
        wheel.Mesh.SetPositionAndRotation(wheelPosition, wheelRotation);
    }

    private IEnumerator GainAcceleration(float acceleration, int period)
    {
        SetAcceleration(acceleration);
        yield return new WaitForSeconds(period);
        SetAcceleration(0f);
        _moveCoroutine = null;
    }

    private IEnumerator StopCar()
    {
        float timer = _rigidbody.velocity.magnitude / 3.0f;
        SetAcceleration(0f);

        do
        {
            _rigidbody.velocity = Vector3.Slerp(_rigidbody.velocity, Vector3.zero, timer *  Time.deltaTime);
            yield return null;
        } while (_wheelRearLeft.Collider.isGrounded && _rigidbody.velocity.magnitude > 0.001f);

        _rigidbody.velocity = Vector3.zero;
        _breakCoroutine = null;
    }

    private void SetAcceleration(float torqueScale)
    {
        float torque = torqueScale * _wheelTorque;
        _wheelRearLeft.Collider.motorTorque = torque;
        _wheelRearRight.Collider.motorTorque = torque;
    }

    public void StartMoving()
    {
        if (!_started && _wheelRearLeft.Collider.isGrounded && _moveCoroutine == null)
        {
            _moveCoroutine = StartCoroutine(GainAcceleration(_moveSpeed, 3));
            _started = true;
        }
    }
    
    public void Accelerate()
    {
        if(_started && _wheelRearLeft.Collider.isGrounded && _moveCoroutine == null && _breakCoroutine == null)
            _moveCoroutine = StartCoroutine(GainAcceleration(_rigidbody.velocity.magnitude * 0.05f, 1));
    }

    public void Brake()
    {
        if (_rigidbody.velocity.magnitude > 0.01f)
        {
            _started = false;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (_wheelRearLeft.Collider.isGrounded)
                _breakCoroutine = StartCoroutine(StopCar());
        }
        else
            _moveCoroutine = StartCoroutine(GainAcceleration(-_moveBackSpeed, 1));
    }
}
