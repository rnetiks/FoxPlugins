#pragma kernel Fill

RWTexture2D<float4> OutputTexture;

float4 _Color1;
[threadgroup_size(16, 16, 1)]
void Fill(uint2 id : SV_DispatchThreadID)
{
    OutputTexture[id.xy] = _Color1;
}