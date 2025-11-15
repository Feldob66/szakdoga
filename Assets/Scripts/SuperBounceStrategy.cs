using UnityEngine;

public class SuperBounceStrategy : IBounceStrategy
{
    public Vector3 CalculateBounce(Vector3 incomingVelocity, Vector3 normal)
    {
        return Vector3.Reflect(incomingVelocity, normal) * 1.5f;
    }
}
