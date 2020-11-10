using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeRenderer : MonoBehaviour
{
    // This is the rate at which the hologram position is interpolated (LERPed) to the current location.
    float _lerpRate = 4.0f;
    float m_degreesPerSecond = 45.0f;
    System.Numerics.Vector3 _targetPosition = new System.Numerics.Vector3(0.0f, 0.0f, -2.0f);
    System.Numerics.Vector3 _position = new System.Numerics.Vector3(0.0f, 0.0f, -2.0f);
    System.Numerics.Vector3 _velocity = new System.Numerics.Vector3(0.0f, 0.0f, 0.0f);

    // Update is called once per frame
    public void Update()
    {
        float deltaTime = Time.deltaTime;
        float lerpDeltaTime = deltaTime * _lerpRate;

        System.Numerics.Vector3 prevPosition = _position;
        _position = System.Numerics.Vector3.Lerp(_position, _targetPosition, lerpDeltaTime);
        transform.position = _position.ToUnity();

        _velocity = (prevPosition - _position) / deltaTime;
    }

    public System.Numerics.Vector3 GetPosition() { return _position; }
    public System.Numerics.Vector3 GetVelocity() { return _velocity; }

    public void SetTargetPosition(System.Numerics.Vector3 pos) { _targetPosition = pos; }
}
