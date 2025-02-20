using UnityEngine;

public static class StepClimbUtility
{
    public static float StepClimb(this Vector3 origin, float stepHeight, float stepDistance, float radius, LayerMask layers)
    {
        if (Physics.SphereCast(origin + Vector3.up * stepHeight, radius + stepDistance, Vector3.down, out RaycastHit hit, stepHeight - radius - stepDistance, layers, QueryTriggerInteraction.Ignore)
            && !Physics.SphereCast(origin + Vector3.up * stepHeight, radius + stepDistance, Vector3.up, out _, stepHeight, layers, QueryTriggerInteraction.Ignore))
            return hit.point.y - origin.y;
        return -1f;
    }
}
