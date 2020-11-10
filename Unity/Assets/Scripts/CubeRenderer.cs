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

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    public void Update()
    {
        float deltaTime = Time.deltaTime;
        float lerpDeltaTime = deltaTime * _lerpRate;

        System.Numerics.Vector3 prevPosition = _position;
        _position = System.Numerics.Vector3.Lerp(_position, _targetPosition, lerpDeltaTime);
        transform.position = _position.ToUnity();

        _velocity = (prevPosition - _position) / deltaTime;

        /*
        // Rotate the cube.
        // Convert degrees to radians, then convert seconds to rotation angle.
        //float radiansPerSecond = XMConvertToRadians(m_degreesPerSecond);
        float totalRotation    = static_cast<float>(timer.GetTotalSeconds()) * radiansPerSecond;

        // Scale the cube down to 10cm
        float4x4 const modelScale = make_float4x4_scale({ 0.1f });
        float4x4 const modelRotation = make_float4x4_rotation_y(totalRotation);
        float4x4 const modelTranslation = make_float4x4_translation(m_position);

        m_modelConstantBufferData.model = modelScale * modelRotation * modelTranslation;

        // Use the D3D device context to update Direct3D device-based resources.
        const auto context = m_deviceResources->GetD3DDeviceContext();

        // Update the model transform buffer for the hologram.
        context->UpdateSubresource(
            m_modelConstantBuffer.Get(),
            0,
            nullptr,
            &m_modelConstantBufferData,
            0,
            0
        );*/
    }

    public System.Numerics.Vector3 GetPosition() { return _position; }
    public System.Numerics.Vector3 GetVelocity() { return _velocity; }

    public void SetTargetPosition(System.Numerics.Vector3 pos) { _targetPosition = pos; }
}
