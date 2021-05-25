using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Sphere")]
public class SettingsSphere : ScriptableObject
{

	[Range(0f, 100f)]
	public float maxSpeed = 10f;

	[Range(0f, 100f)]
	public float maxAcceleration = 10f;

	[Range(0f, 100f)]
	public float maxAirAcceleration = 1f;

	[Range(0f, 10f)]
	public float jumpHeight = 2f;

	[Range(0, 5)]
	public int maxAirJumps = 0;

	[Range(0f, 90f)]
	public float maxGroundAngle = 25f, maxStairsAngle = 50f;

	[Range(0f, 100f)]
	public float maxSnapSpeed = 100f;

	[Min(0f)]
	public float probeDistance = 1f;

	public LayerMask probeMask = -1, stairsMask = -1;
}
