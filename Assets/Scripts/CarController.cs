using System.Collections;
using UnityEngine;

public enum CarStates
{
    IDLE,
    STOPED,
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
    [SerializeField] private float _stopParam;

    private bool _isGrounded;
    private float _currentAcceleration;
    private float _currentSpeed;
    private CarStates _state;
    private Rigidbody _rigidbody;

    #region Standart Unity Functions
    private void Start()
    {
        _isGrounded = false;

        _state = CarStates.IDLE;
        _currentAcceleration = _minMoveSpeed;
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out SpeedPlatform speedPlatform))
        {
            float force = _currentSpeed * speedPlatform.SpeedScaler * _rigidbody.mass;
            _rigidbody.AddForce(force * other.transform.forward, ForceMode.Impulse);
        }
    }

    // In case we colliding with wall - we want to be able to start move again.
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Wall _) && _wheelRearLeft.Collider.rpm > _stopParam)
        {
            SetBrakers(Mathf.Infinity);
            _state = CarStates.STOPED;
        }
    }

    private void FixedUpdate()
    {
        _currentSpeed = _rigidbody.velocity.magnitude;
        _isGrounded = _wheelRearLeft.Collider.isGrounded && _wheelRearRight.Collider.isGrounded;
        
        UpdateWheelVisuals();
        
        // We can contiously moving without acceleration and when the speed reaches 0 - we are still in idle, so we need to
        // start braking for 3 seconds before we can move back (that's not a good situation). So we checking if in the idle
        // state we reached almost zero veclocity.
        if(_state == CarStates.IDLE && _currentSpeed < 0.1f)
        {
            _state = CarStates.STOPED;
            SetBrakers(Mathf.Infinity);
        }

        // Uncomment this to display car state in the debug mode.
        //Debug.Log(_state);
    }
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
    private IEnumerator Accelerating()
    {
        _state = CarStates.ACCELERATING;

        while(_state == CarStates.ACCELERATING)
        {
            _currentAcceleration = _currentSpeed * 1.05f;
            yield return new WaitForSeconds(1);
        }
        
        SetAcceleration(0f);
    }

    private IEnumerator Moving()
    {
        _state = CarStates.MOVING;

        while ((_state == CarStates.MOVING || _state == CarStates.ACCELERATING) && _isGrounded)
        {
            if (_currentSpeed < _currentAcceleration)
                SetAcceleration(_currentAcceleration);
            else
                SetAcceleration(0f);
            yield return null;
        }

        SetAcceleration(0f);
        _currentAcceleration = _minMoveSpeed;
    }

    private IEnumerator Brake()
    {
        _state = CarStates.BREAKING;
        float timer = 0.0f;

        // Counting 2 seconds instead of 3, because we have a wheel based movement, so little force will last on the wheels
        // and we will need manually set brakers to discard of those forces
        while (timer <= 2.0f && _state == CarStates.BREAKING && _isGrounded)
        {
            _rigidbody.velocity = Vector3.Slerp(_rigidbody.velocity, Vector3.zero, 3f * timer * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        // Setting brakers to discard of forces applied to wheels only if we not moving again
        if (_state == CarStates.BREAKING)
        {
            SetBrakers(Mathf.Infinity);
            _state = CarStates.STOPED;
        }
    }

    private IEnumerator MovingBack()
    {
        _state = CarStates.MOVING_BACK;
        
        while (_state == CarStates.MOVING_BACK && _isGrounded)
        {
            if (_currentSpeed < _moveBackSpeed && _rigidbody.velocity != (transform.forward * -_moveBackSpeed))
            {
                if (_wheelRearLeft.Collider.rpm < _stopParam && _wheelRearRight.Collider.rpm < _stopParam)
                {
                    SetBrakers(0f);
                    SetAcceleration(-_moveBackSpeed);
                }
            }
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
        if (_isGrounded && _state == CarStates.MOVING)
            StartCoroutine(Accelerating());
    }

    public void Move()
    {
        if (_isGrounded && _state != CarStates.MOVING && _state != CarStates.ACCELERATING)
        {
            SetBrakers(0f);
            StartCoroutine(Moving());
        }
    }

    public void StopAccelerating()
    {
        if (_state == CarStates.MOVING || _state == CarStates.ACCELERATING)
        {
            SetBrakers(_wheelTorque * _minMoveSpeed);
            _state = CarStates.IDLE;
        }
    }

    public void BrakeOrMoveBack()
    {
        if(_isGrounded)
        {        
            if(_state == CarStates.STOPED)
                StartCoroutine(MovingBack());
            else if (_state != CarStates.MOVING_BACK && _state != CarStates.BREAKING)
                StartCoroutine(Brake());
        }
    }

    public void StopMovingBack()
    {
        if(_state == CarStates.MOVING_BACK)
        {
            SetBrakers(Mathf.Infinity);
            _state = CarStates.STOPED;
        }
    }

    public void HandleSteering(float angle)
    {
        // Compensation during turn
        if (angle != 0.0f && (_state == CarStates.MOVING || _state == CarStates.ACCELERATING))
            _rigidbody.velocity = Vector3.Slerp(_rigidbody.velocity, (transform.right * angle + transform.forward) * 6f, Time.deltaTime);

        angle *= _maxSteeringAngle;

        _wheelFrontRight.Collider.steerAngle = angle;
        _wheelFrontLeft.Collider.steerAngle = angle;
    }
    #endregion
}
