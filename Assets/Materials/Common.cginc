void CircularGradient_float(float4 c1, float4 c2, float2 uv, out float4 colour)
{
    float2 d = uv - float2(0.5, 0.5);
    float r = length(d) * 2.0;

    colour = lerp(c1, c2, r);
}

void Float3ToXZ_float(float3 input, out float2 result)
{
    result = float2(input.x, input.z);
}