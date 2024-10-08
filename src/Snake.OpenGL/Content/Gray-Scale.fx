﻿#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float percent;

sampler2D TextureSampler : register(s0)
{
    Texture = (Texture);
};

float4 MainPS(float4 position : SV_Position, float4 color : COLOR0, float2 TextureCoordinates : TEXCOORD0) : COLOR0
{
    float4 col = tex2D(TextureSampler, TextureCoordinates) * color;
    col.rgb = lerp(col, col.r * 0.299 + col.g * 0.587 + col.b * 0.114, percent); // grey scale
    return col;
}

technique BasicColorDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};