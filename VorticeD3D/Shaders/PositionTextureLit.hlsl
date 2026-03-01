cbuffer TransformBuffer : register(b0)
{
    row_major float4x4 ModelView;
    row_major float4x4 Projection;
};

cbuffer LightBuffer : register(b1)
{
    float4 Light0Position;
    float4 Light0Ambient;
    float4 Light0Diffuse;
    float4 Light1Position;
    float4 Light1Ambient;
    float4 Light1Diffuse;
    float4 LightFlags; // x = light0On, y = light1On
};

Texture2D diffuseTexture : register(t0);
SamplerState textureSampler : register(s0);

struct VS_INPUT
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float4 vertColor : COLOR;
};

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR;
};

PS_INPUT VS(VS_INPUT input)
{
    PS_INPUT output;
    float4 worldPos = mul(float4(input.Position, 1.0), ModelView);
    output.Position = mul(worldPos, Projection);
    output.Position.z = (output.Position.z + output.Position.w) * 0.5;
    output.TexCoord = input.TexCoord;

    float3 viewNormal = normalize(mul(input.Normal, (float3x3)ModelView));
    float3 litColor = float3(0.2, 0.2, 0.2); // Global ambient

    if (LightFlags.x > 0.5)
    {
        float3 lightDir = normalize(Light0Position.xyz);
        float ndotl = max(0, dot(viewNormal, lightDir));
        litColor += Light0Ambient.rgb + Light0Diffuse.rgb * ndotl;
    }

    if (LightFlags.y > 0.5)
    {
        float3 lightDir = normalize(Light1Position.xyz);
        float ndotl = max(0, dot(viewNormal, lightDir));
        litColor += Light1Ambient.rgb + Light1Diffuse.rgb * ndotl;
    }

    output.Color = float4(min(1, input.vertColor.rgb * litColor), input.vertColor.a);
    return output;
}

float4 PS(PS_INPUT input) : SV_TARGET
{
    float4 texColor = diffuseTexture.Sample(textureSampler, input.TexCoord);
    return texColor * input.Color;
}
