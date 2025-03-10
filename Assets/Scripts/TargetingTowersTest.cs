using NUnit.Framework;
using System.Collections;
using UnityEngine;

public class TargetingTowersTest : MonoBehaviour
{

    public Transform target; // Target GameObject (assign in Inspector)
    public float launchVelocity = 10f; // Initial velocity
    public float gravity = 9.8f; // Gravity value

    private void Start()
    {
        // Calculate the launch direction as a normalized Vector3

            Debug.Log($"Normalized launch direction: {CalculateLaunchDirection(transform.position, target.position, launchVelocity)}");
    }

    private Vector3 CalculateLaunchDirection(Vector3 start, Vector3 target, float velocity)
    {
        Vector3 displacement = target - start;
        float horizontalDistance = new Vector3(displacement.x, 0, displacement.z).magnitude;
        float verticalDistance = displacement.y;

        float velocitySquared = velocity * velocity;
        float determinant = velocitySquared * velocitySquared - gravity * (gravity * horizontalDistance * horizontalDistance + 2 * verticalDistance * velocitySquared);

        if (determinant < 0)
            return Vector3.zero; // No valid launch direction

        float sqrtDet = Mathf.Sqrt(determinant);
        float angle = Mathf.Atan2(velocitySquared - sqrtDet, gravity * horizontalDistance); // Using the lower angle

        Vector3 horizontalDirection = new Vector3(displacement.x, 0, displacement.z).normalized;
        float horizontalSpeed = Mathf.Cos(angle) * velocity;
        float verticalSpeed = Mathf.Sin(angle) * velocity;

        return (horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed).normalized;
    }
}
