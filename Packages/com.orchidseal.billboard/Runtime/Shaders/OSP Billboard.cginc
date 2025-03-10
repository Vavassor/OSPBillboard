#ifndef OSP_BILLBOARD_CGINC_
#define OSP_BILLBOARD_CGINC_

#include <UnityShaderVariables.cginc>

// This file uses the following shader features.
// #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_SPHERICAL _BILLBOARD_MODE_CYLINDRICAL_WORLD _BILLBOARD_MODE_CYLINDRICAL_LOCAL
//
// Which can also be added as a shader property.
// [KeywordEnum(None, Spherical, Cylindrical_World, Cylindrical_Local)] _Billboard_Mode("Billboard Mode", Float) = 1

float3 GetCenterCameraPosition()
{
    #if defined(USING_STEREO_MATRICES)
    float3 worldPosition = (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) / 2.0;
    #else
    float3 worldPosition = _WorldSpaceCameraPos.xyz;
    #endif
    return worldPosition;
}

float3x3 LookAtVerticalMatrix(float3 forward, float3 up)
{
    float3 xAxis = normalize(cross(up, forward));
    float3 yAxis = up;
    float3 zAxis = forward;
    return float3x3(
        xAxis.x, yAxis.x, zAxis.x,
        xAxis.y, yAxis.y, zAxis.y,
        xAxis.z, yAxis.z, zAxis.z
    );
}

float3x3 LookAtMatrix(float3 forward, float3 up)
{
    float3 zAxis = forward;
    float3 xAxis = normalize(cross(up, zAxis));
    float3 yAxis = normalize(cross(zAxis, xAxis));
    return float3x3(
        xAxis.x, yAxis.x, zAxis.x,
        xAxis.y, yAxis.y, zAxis.y,
        xAxis.z, yAxis.z, zAxis.z
    );
}

bool IsInMirror()
{
    return unity_CameraProjection[2][0] != 0.0f || unity_CameraProjection[2][1] != 0.0f;
}

float3x3 GetBillboardRotation(float3 objectCenterWs)
{
    float3x3 rotation;
    #if _BILLBOARD_MODE_CYLINDRICAL_LOCAL
        float3 upAxis = normalize(unity_ObjectToWorld._m01_m11_m21);
        float3 forwardWs = normalize(objectCenterWs - GetCenterCameraPosition());
        if (IsInMirror())
        {
            forwardWs = mul((float3x3) unity_WorldToObject, unity_CameraWorldClipPlanes[5].xyz);
        }
        rotation = LookAtVerticalMatrix(forwardWs, upAxis);
    #elif _BILLBOARD_MODE_CYLINDRICAL_WORLD
        float3 forwardWs = normalize(objectCenterWs - GetCenterCameraPosition());
        if (IsInMirror())
        {
            forwardWs = mul((float3x3) unity_WorldToObject, unity_CameraWorldClipPlanes[5].xyz);
        }
        rotation = LookAtVerticalMatrix(forwardWs, float3(0, 1, 0));
    #else
        #if defined(USING_STEREO_MATRICES)
            float3 up = float3(0, 1, 0);
            float3 forward = normalize(objectCenterWs - GetCenterCameraPosition());
        #else
            float3 up = UNITY_MATRIX_V._m10_m11_m12;
            float3 forward = -UNITY_MATRIX_V._m20_m21_m22;
        #endif
        rotation = LookAtMatrix(forward, up);
    #endif
    
    return rotation;
}

float4 BillboardCs(float4 positionOs)
{
    #if _BILLBOARD_MODE_NONE
        return UnityObjectToClipPos(positionOs);
    #else
        float3 objectCenterWs = unity_ObjectToWorld._m03_m13_m23;
        float3x3 rotation = GetBillboardRotation(objectCenterWs);
        return mul(UNITY_MATRIX_VP, float4(mul(rotation, positionOs) + objectCenterWs, 1));
    #endif 
}

float3 BillboardWs(float4 positionOs)
{
    #if _BILLBOARD_MODE_NONE
        return mul(unity_ObjectToWorld, positionOs);
    #else
        float3 objectCenterWs = unity_ObjectToWorld._m03_m13_m23;
        float3x3 rotation = GetBillboardRotation(objectCenterWs);
        return mul(rotation, positionOs) + objectCenterWs;
    #endif 
}

void BillboardWithNormalWs(float4 positionOs, float3 normalOs, out float3 positionWs, out float3 normalWs)
{
    #if _BILLBOARD_MODE_NONE
        positionWs = mul(unity_ObjectToWorld, positionOs);
        normalWs = UnityObjectToWorldNormal(normalOs);
    #else
        float3 objectCenterWs = unity_ObjectToWorld._m03_m13_m23;
        float3x3 rotation = GetBillboardRotation(objectCenterWs);
        positionWs = mul(rotation, positionOs) + objectCenterWs;
        normalWs = normalize(mul(rotation, normalOs));
    #endif 
}

void BillboardWithNormalTangentWs(float4 positionOs, float3 normalOs, float4 tangentOs, out float3 positionWs, out float3 normalWs, out float3 tangentWs)
{
    #if _BILLBOARD_MODE_NONE
        positionWs = mul(unity_ObjectToWorld, positionOs);
        normalWs = UnityObjectToWorldNormal(normalOs);
        tangentWs = UnityObjectToWorldDir(tangentOs.xyz);
    #else
        float3 objectCenterWs = unity_ObjectToWorld._m03_m13_m23;
        float3x3 rotation = GetBillboardRotation(unity_ObjectToWorld._m03_m13_m23);
        positionWs = mul(rotation, positionOs) + objectCenterWs;
        normalWs = normalize(mul(rotation, normalOs));
        tangentWs = normalize(mul(tangentOs.xyz, rotation));
    #endif 
}

#endif // OSP_BILLBOARD_CGINC_
