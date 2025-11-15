using UnityEngine;

public interface IBounceStrategy
{
    Vector3 CalculateBounce(Vector3 incomingVelocity, Vector3 normal);
}
