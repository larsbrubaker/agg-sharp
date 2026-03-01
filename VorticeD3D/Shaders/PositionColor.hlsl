cbuffer TransformBuffer : register(b0)
{
    row_major float4x4 ModelView;
    row_major float4x4 Projection;
};

struct VS_INPUT
{
    float3 Position : POSITION;
    float4 vertColor : COLOR;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    float4 worldPos = mul(float4(input.Position, 1.0), ModelView);
    output.Position = mul(worldPos, Projection);
    output.Position.z = (output.Position.z + output.Position.w) * 0.5;
    output.Color = input.vertColor;
    return output;
}

float4 PS(PS_INPUT input) : SV_TARGET
{
    return input.Color;
}
