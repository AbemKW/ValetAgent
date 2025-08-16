using UnityEngine;

[ExecuteInEditMode] // Optional: Ensures gizmos update in Edit Mode
public class SpawnPoint : MonoBehaviour
{
    public Color gizmoColor = Color.yellow; // Color of the gizmo spheres
    public float sphereRadius = 0.2f;      // Radius of the sphere gizmo

    private void OnDrawGizmos()
    {
        // Check if there are any children
        if (transform.childCount == 0) return;

        Gizmos.color = gizmoColor;

        // Loop through all child transforms
        foreach (Transform child in transform)
        {
            Gizmos.DrawSphere(child.position, sphereRadius);
        }
    }
}