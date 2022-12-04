using UnityEngine;

public class SpeedPlatform : MonoBehaviour
{
    [SerializeField] private float _speedScaler = 0.15f;

    public float SpeedScaler => _speedScaler;
}