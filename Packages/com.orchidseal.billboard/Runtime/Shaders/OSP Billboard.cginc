#ifndef OSP_BILLBOARD_CGINC_
#define OSP_BILLBOARD_CGINC_

#include <UnityShaderVariables.cginc>

// This file uses the following shader features.
// #pragma shader_feature_local _BILLBOARD_MODE_NONE _BILLBOARD_MODE_AUTO _BILLBOARD_MODE_VIEW _BILLBOARD_MODE_WORLD_Y _BILLBOARD_MODE_LOCAL_Y
//
// Which can also be added as a shader property.
// [KeywordEnum(None, Auto, View, World_Y, Local_Y)] _Billboard_Mode("Billboard Mode", Float) = 1

float3 GetCenterCameraPosition()
{
    #if defined(USING_STEREO_MATRICES)
    float3 worldPosition = (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]) / 2.0;
    #else
    float3 worldPosition = _WorldSpaceCameraPos.xyz;
    #endif
    return worldPosition;
}

float4x4 LookAtMatrix(float3 forward, float3 up)
{
    float3 xAxis = normalize(cross(forward, up));
    float3 yAxis = up;
    float3 zAxis = forward;
    return float4x4(
        xAxis.x, yAxis.x, zAxis.x, 0,
        xAxis.y, yAxis.y, zAxis.y, 0,
        xAxis.z, yAxis.z, zAxis.z, 0,
        0, 0, 0, 1
        );
}

bool IsInMirror()
{
    return unity_CameraProjection[2][0] != 0.0f || unity_CameraProjection[2][1] != 0.0f;
}

float4 BillboardVerticalCs(float4 positionOs, float3 objectCenterWs, float4x4 inverseModel, float3 upAxis)
{
    float3 viewDirectionWs = objectCenterWs - GetCenterCameraPosition();
    if (IsInMirror())
    {
        viewDirectionWs = mul((float3x3) inverseModel, unity_CameraWorldClipPlanes[5].xyz);
    }
    float3 positionWs = mul(LookAtMatrix(-viewDirectionWs, float4(upAxis, 0)), positionOs) + objectCenterWs.xyz;
    return mul(UNITY_MATRIX_VP, float4(positionWs, 1.0));
}

float4 BillboardVerticalWs(float4 positionOs, float3 objectCenterWs, float4x4 inverseModel, float3 upAxis)
{
    float3 viewDirectionWs = objectCenterWs - GetCenterCameraPosition();
    if (IsInMirror())
    {
        viewDirectionWs = mul((float3x3) inverseModel, unity_CameraWorldClipPlanes[5].xyz);
    }
    float3 positionWs = mul(LookAtMatrix(-viewDirectionWs, float4(upAxis, 0)), positionOs) + objectCenterWs.xyz;
    return float4(positionWs, 1.0);
}

float4 BillboardViewCs(float4 positionOs, float4x4 modelView)
{
    float4 positionVs = mul(modelView, float4(0, 0, 0, 1)) + float4(positionOs.xyz, 0);
    return mul(UNITY_MATRIX_P, positionVs);
}

float4 BillboardViewWs(float4 positionOs, float4x4 modelView)
{
    float4 positionVs = mul(modelView, float4(0, 0, 0, 1)) + float4(positionOs.xyz, 0);
    float4 newPositionWs = mul(UNITY_MATRIX_I_V, positionVs);
    newPositionWs /= newPositionWs.w;
    return newPositionWs;
}

float4 Billboard(float4 positionOs)
{
    #if _BILLBOARD_MODE_NONE
        return UnityObjectToClipPos(positionOs);
    #elif (_BILLBOARD_MODE_AUTO && defined(USING_STEREO_MATRICES) || _BILLBOARD_MODE_WORLD_Y || _BILLBOARD_MODE_LOCAL_Y)
        #if _BILLBOARD_MODE_LOCAL_Y
        float3 upAxis;
        upAxis.x = unity_ObjectToWorld[0][1];
        upAxis.y = unity_ObjectToWorld[1][1];
        upAxis.z = unity_ObjectToWorld[2][1];
        #else
        float3 upAxis = float3(0, 1, 0);
        #endif
        float3 objectCenterWs;
        objectCenterWs.x = unity_ObjectToWorld[0][3];
        objectCenterWs.y = unity_ObjectToWorld[1][3];
        objectCenterWs.z = unity_ObjectToWorld[2][3];
        return BillboardVerticalCs(positionOs, objectCenterWs, unity_WorldToObject, upAxis);
    #else
        return BillboardViewCs(positionOs, UNITY_MATRIX_MV);
    #endif
}

float4 BillboardWs(float4 positionOs)
{
    #if _BILLBOARD_MODE_NONE
    return mul(unity_ObjectToWorld, positionOs);
    #elif (_BILLBOARD_MODE_AUTO && defined(USING_STEREO_MATRICES) || _BILLBOARD_MODE_WORLD_Y || _BILLBOARD_MODE_LOCAL_Y)
    #if _BILLBOARD_MODE_LOCAL_Y
    float3 upAxis;
    upAxis.x = unity_ObjectToWorld[0][1];
    upAxis.y = unity_ObjectToWorld[1][1];
    upAxis.z = unity_ObjectToWorld[2][1];
    #else
    float3 upAxis = float3(0, 1, 0);
    #endif
    float3 objectCenterWs;
    objectCenterWs.x = unity_ObjectToWorld[0][3];
    objectCenterWs.y = unity_ObjectToWorld[1][3];
    objectCenterWs.z = unity_ObjectToWorld[2][3];
    return BillboardVerticalWs(positionOs, objectCenterWs, unity_WorldToObject, upAxis);
    #else
    return BillboardViewWs(positionOs, UNITY_MATRIX_MV);
    #endif
}

#endif // OSP_BILLBOARD_CGINC_
