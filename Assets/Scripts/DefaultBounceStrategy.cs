using UnityEngine;

public class DefaultBounceStrategy : IBounceStrategy
{
    public Vector3 CalculateBounce(Vector3 incomingVelocity, Vector3 normal)
    {
        return Vector3.Reflect(incomingVelocity, normal);
    }
}