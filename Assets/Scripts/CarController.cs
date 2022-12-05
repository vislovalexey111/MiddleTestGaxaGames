using System.Collections;
using UnityEngine;

public enum CarStates
{
    IDLE,
    ACCELERATING,
    MOVING,
    MOVING_BACK,
    BREAKING
}

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
    [SerializeField] private float _minMoveSpeed;

    private CarStates _state;
    private Rigidbody _rigidbody;
    private Coroutine _moveCoroutine;
    private bool _steering;

    #region Standart Unity Functions
    private void Start()
    {
        _state = CarStates.IDLE;
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out SpeedPlatform speedPlatform))
            _rigidbody.velocity += _rigidbody.velocity.magnitude * speedPlatform.SpeedScaler * other.transform.forward;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Wall _))
        {
            StopMoving();
            _state = CarStates.IDLE;
        }
    }

    private void FixedUpdate() => UpdateWheelVisuals();
    #endregion

    #region Hellper Functions
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

    private void StopMoving()
    {
        SetAcceleration(0f);

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }

    private void SetAcceleration(float torqueScale)
    {
        float torque = torqueScale * _wheelTorque;
        _wheelRearLeft.Collider.motorTorque = torque;
        _wheelRearRight.Collider.motorTorque = torque;
    }

    private void SetBrakers(float brakeForce)
    {
        _wheelRearLeft.Collider.brakeTorque = brakeForce;
        _wheelRearRight.Collider.brakeTorque = brakeForce;
    }
    #endregion

    #region Movement Enumerators
    private IEnumerator StartAcceleration()
    {
        float acceleration = (_rigidbody.velocity.magnitude < _minMoveSpeed) ? _minMoveSpeed : _rigidbody.velocity.magnitude * 0.05f;
        SetAcceleration(acceleration);
        yield return new WaitForSeconds(1);
        SetAcceleration(0f);

        _moveCoroutine = null;
    }

    private IEnumerator StartMoving()
    {
        float timer = 0.0f;
        _state = CarStates.MOVING;

        // Accelerating first 2 seconds to gain movement
        while (timer <= 2.0f && _state == CarStates.MOVING)
        {
            timer += Time.deltaTime;
            SetAcceleration(1);
            yield return null;
        }

        SetAcceleration(0f);
        _state = CarStates.ACCELERATING;
        _moveCoroutine = null;
    }

    private IEnumerator StartBraking()
    {
        StopMoving();
        _state = CarStates.BREAKING;
        float timer = 0.0f;

        // Checking for 2 instead of 3, because we have a wheel based movement, so little force will last on the wheels
        // and we will need manually set brakers
        while (timer <= 2.0f && _state == CarStates.BREAKING && _wheelRearLeft.Collider.isGrounded && _wheelRearRight.Collider.isGrounded)
        {
            _rigidbody.velocity = Vector3.Slerp(_rigidbody.velocity, Vector3.zero, 3f * timer * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // Setting brakers to discard of forces applied to wheels
        SetBrakers(Mathf.Infinity);
        _state = CarStates.IDLE;
    }

    private IEnumerator StartMovingBack()
    {
        SetBrakers(0f);
        _state = CarStates.MOVING_BACK;

        while (_state == CarStates.MOVING_BACK)
        {
            if (_rigidbody.velocity.magnitude < _moveBackSpeed)
                SetAcceleration(-_moveBackSpeed);
            else
                SetAcceleration(0);
            yield return null;
        }

        SetAcceleration(0f);
    }
    #endregion

    #region Controller Interface
    public void Accelerate()
    {
        if (_wheelRearLeft.Collider.isGrounded && _wheelRearRight.Collider.isGrounded && _moveCoroutine == null)
        {
            SetBrakers(0f); // in case of breaking interrupt

            if (_state == CarStates.ACCELERATING)
                _moveCoroutine = StartCoroutine(StartAcceleration());
            else
                _moveCoroutine = StartCoroutine(StartMoving());
        }
    }

    public void Brake()
    {
        if(_state == CarStates.IDLE)
            StartCoroutine(StartMovingBack());
        else if (_state != CarStates.BREAKING)
            StartCoroutine(StartBraking());
    }

    public void HandleSteering(float angle)
    {
        _steering = angle != 0.0f;

        // Compensation during turn
        if (_steering && (_state == CarStates.MOVING || _state == CarStates.ACCELERATING))
            _rigidbody.velocity += (transform.right * angle + transform.forward)  * Time.deltaTime;

        angle *= _maxSteeringAngle;

        _wheelFrontRight.Collider.steerAngle = angle;
        _wheelFrontLeft.Collider.steerAngle = angle;
    }
    #endregion
}
