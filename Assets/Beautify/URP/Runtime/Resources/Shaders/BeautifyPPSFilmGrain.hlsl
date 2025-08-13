#ifndef BEAUTIFY_PPS_FILM_GRAIN
#define BEAUTIFY_PPS_FILM_GRAIN

float4 _FilmGrainData; // x: intensity, y: luma attenuation, z: resolution, w: unused
float4 _FilmArtifactsData; // x: dirt spots amount, y: dirt spots intensity, z: scratches amount, w: scratches intensity

#define FILM_GRAIN_INTENSITY _FilmGrainData.x
#define FILM_GRAIN_LUMA_ATTENUATION _FilmGrainData.y
#define FILM_GRAIN_RESOLUTION _FilmGrainData.z

#define DIRT_SPOTS_AMOUNT _FilmArtifactsData.x
#define DIRT_SPOTS_INTENSITY _FilmArtifactsData.y
#define SCRATCHES_AMOUNT _FilmArtifactsData.z
#define SCRATCHES_INTENSITY _FilmArtifactsData.w

float3 r3(float2 uv) {
    static const float2 magic1 = float2(321.8942, 1225.6548);
    static const float magic2 = 4251.4865;
    float d1 = dot(uv, magic1);
    float d2 = dot(uv + 0.1, magic1);
    float d3 = dot(uv + 0.2, magic1);
    return frac(sin(float3(d1, d2, d3)) * magic2 + _Time.y);
}

inline float r1(float x) {
    static const float magic = 4251.4865;
    return frac(sin(x * 321.8942) * magic + _Time.y);
}


// Generate film artifacts like dirt spots and scratches
float getFilmArtifacts(float2 uv) {
    float artifacts = 0.0;
    
    // Dirt spots - small circular spots that appear randomly
    UNITY_BRANCH
    if (DIRT_SPOTS_AMOUNT > 0) {
        float2 dirtUV = uv * 20.0;
        float2 dirtCell = floor(dirtUV);
        float2 dirtFrac = dirtUV - dirtCell;
        float3 dirtRandom = r3(dirtCell + floor(_Time.y * 0.5));
        if (dirtRandom.x > DIRT_SPOTS_AMOUNT) {
            float2 dirtOffset = dirtFrac - 0.5;
            float dirtDistanceSqr = dot(dirtOffset, dirtOffset);
            float dirtSize = dirtRandom.y * 0.3 + 0.1;
            float dirtSpot = 1.0 - smoothstep(0.0, dirtSize * dirtSize, dirtDistanceSqr);
            artifacts += dirtSpot * (dirtRandom.z - 0.5) * DIRT_SPOTS_INTENSITY;
        }
    }
    
    // Vertical scratches - appear occasionally and persist for a few frames
    UNITY_BRANCH
    if (SCRATCHES_AMOUNT > 0) {
        float scratchTime = floor(dot(_Time.xyzw, 1.0));
        float scratchRandom = r1(scratchTime);
        if (scratchRandom > SCRATCHES_AMOUNT) {
            float scratchX = r1(scratchTime + 0.1);
            float scratchWidth = 0.001 + r1(scratchTime + 0.2) * 0.003;
            float scratchDistance = abs(uv.x - scratchX);
            float scratch = 1.0 - smoothstep(0.0, scratchWidth, scratchDistance);
            artifacts += scratch * SCRATCHES_INTENSITY;
        }
    }

    return artifacts;
}

void ApplyFilmGrain(inout float3 rgb, float luma, float2 uv) {

    float2 noiseSize = _MainTex_TexelSize.zw * FILM_GRAIN_RESOLUTION;
    float2 scaledUV = uv * noiseSize;
    float2 cell     = floor(scaledUV);

    #if BEAUTIFY_TURBO
        static const float c0 = 1.0f;
        static const float c1 = -1.828427f;
        static const float c2 = 0.828427f;
        float luminanceFactor = c0 + luma * (c1 + luma * c2);
        float3 grain  = r3(cell / noiseSize);
        float artifacts = 0.0;
    #else
        float luminanceFactor = 1.0 - sqrt(luma);
        // Bilinear interpolation
        float2 fracUV   = scaledUV - cell;
        float2 invNoiseSize = rcp(noiseSize);
        float3 rand00 = r3(cell * invNoiseSize);
        float3 rand10 = r3((cell + float2(1, 0)) * invNoiseSize);
        float3 rand01 = r3((cell + float2(0, 1)) * invNoiseSize);
        float3 rand11 = r3((cell + float2(1, 1)) * invNoiseSize);
        float3 randX0 = lerp(rand00, rand10, fracUV.x);
        float3 randX1 = lerp(rand01, rand11, fracUV.x);
        float3 grain  = lerp(randX0,  randX1,  fracUV.y);
        float artifacts = getFilmArtifacts(uv);
    #endif

    static const float3 channelSensibility = float3(1, 0.95, 1.05);
    grain = (grain - 0.5) * channelSensibility;

    float grainAmount = lerp(1.0, luminanceFactor, FILM_GRAIN_LUMA_ATTENUATION);
    
    rgb *= 1.0 + grain * grainAmount * FILM_GRAIN_INTENSITY + artifacts;

    rgb = saturate(rgb);
}

#endif // BEAUTIFY_PPS_FILM_GRAIN 