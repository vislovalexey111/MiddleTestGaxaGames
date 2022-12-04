using TMPro;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class CarSpeedViewer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _speedDisplay;

    private Rigidbody _rigidbody;

    private void Start() => _rigidbody = GetComponent<Rigidbody>();
    private void FixedUpdate() => _speedDisplay.text = ((int)_rigidbody.velocity.magnitude).ToString();
}
