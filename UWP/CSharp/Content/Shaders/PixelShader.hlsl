// Per-pixel color data passed through the pixel shader.
struct PixelShaderInput
{
    float4 pos   : SV_POSITION;
    float3 color : COLOR0;
};

// The pixel shader passes through the color data. The color data from 
// is interpolated and assigned to a pixel at the rasterization step.
float4 main(PixelShaderInput input) : SV_TARGET
{
    return float4(input.color, 1.0f);
}
