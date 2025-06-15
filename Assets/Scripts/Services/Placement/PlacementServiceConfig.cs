using UnityEngine;

[CreateAssetMenu(fileName = "PlacementServiceConfig", menuName = "SupermarketSim/Placement Service Config", order = 0)]
public class PlacementServiceConfig : ScriptableObject
{
    [Header("Collision Settings")]
    [Tooltip("Layers that the placement system will check for collisions against when determining valid placement.")]
    [SerializeField]
    private LayerMask _collisionCheckLayers = Physics.DefaultRaycastLayers;
    public LayerMask CollisionCheckLayers => _collisionCheckLayers;

    [Header("Rotation Settings")]
    [Tooltip("Angle in degrees for each rotation step of the placement preview.")]
    [SerializeField]
    private float _rotationStepAngle = 45f;
    public float RotationStepAngle => _rotationStepAngle;

    // Add other placement-related configurations here in the future if needed
    // e.g.:
    // [Header("Preview Settings")]
    // public Color validPlacementColor = new Color(0, 1, 0, 0.5f);
    // public Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);
} 