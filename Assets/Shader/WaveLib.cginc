//float4 _Range;
//float _Speed;
//float _Radius;
//float _Height;
//float _DxDz;
#define PI 3.1415926


float3 RepeatPosition( float3 pos , float4 rangeSize )
{
    float2 res = ( pos.xz + rangeSize.xy ) / ( rangeSize.xy * 2 );
    res = frac(res);
    res = res * ( rangeSize.xy * 2 ) - rangeSize.xy;

    return float3(res.x,pos.y,res.y);
}



// get offset from A to B
float GetOffset( float posA , float posB , float rangeSize  )
{
    float offset1 = posB - posA;
    float offset2 = posB + rangeSize * 2 - posA;
    float offset3 = posB - rangeSize * 2 - posA;

    float res = lerp( offset1 , offset2 , clamp( (abs(offset1) - abs(offset2)) * 10000.0, 0, 1.0) );
    res = lerp( res , offset3 , clamp( (abs(res) - abs(offset3)) * 10000.0 , 0 , 1.0 ));

    return res;
    //float minOffset = min( min(abs(offset1),abs(offset2)),abs(offset3));

    //return (abs(offset1) == minOffset)? offset1 : ((abs(offset2) == minOffset)? offset2 : offset3);
    // return min( min( abs( posB - posA ) , abs( posB + rangeSize * 2 - posA ) ) , abs( posB - rangeSize * 2 - posA ) );
}


float wave_h_func( float x )
{
    // return pow( x , 3 );
    return 1 + cos( x * PI );
}

float wave_xz_func( float x )
{ 
    return - sin( x * PI );
}


void GetWaveOffset( float3 vertexPos , float3 ppos , float4 range, float radius , float heightAmplitude , float hozOffset , out float height, out float2 xzOffset )
{
    float offsetX = GetOffset( ppos.x , vertexPos.x , range.x );
    float offsetZ = GetOffset( ppos.z , vertexPos.z , range.y );

    // offsetZ *= _DxDz;

    float length = sqrt( offsetX * offsetX + offsetZ * offsetZ );

    float x = clamp( length / radius , 0 , 1.0 );
   

    height = heightAmplitude * wave_h_func(x);

    float2 dir = float2( offsetX , offsetZ ) / length ;
    xzOffset = heightAmplitude * wave_xz_func(x) / radius * dir * hozOffset;
}


//float3 GetSingleParticlePosition()
//{
//    return RepeatPosition( float3( _Time.y * _Speed , 0 , 0 ) , _Range); 
//}

//float GetWaveHeightSingleParticle( float3 vertexPos )
//{
//    float3 ppos = GetSingleParticlePosition();
//    float h;
//    float2 off;
//    GetWaveOffset(vertexPos,ppos, h,  off);

//    return h;
//    // return (sin(_Time.y + vertexPos.x - vertexPos.z)+1.0) * 0.5;
//}