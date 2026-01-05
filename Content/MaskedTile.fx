texture TileTexture;
sampler TileSampler = sampler_state
{
    Texture = <TileTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture MaskTexture;
sampler MaskSampler = sampler_state
{
    Texture = <MaskTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 MainPS(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 tile = tex2D(TileSampler, uv) * color;
    float  mask = tex2D(MaskSampler, uv).a;

    // Apply mask to everything (safe for both straight + premultiplied cases)
    tile *= mask;

    return tile;
}

technique MaskedTile
{
    pass P0
    {
        PixelShader = compile ps_2_0 MainPS();
    }
}
