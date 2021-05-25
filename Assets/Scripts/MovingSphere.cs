using System;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	[SerializeField] private SettingsSphere _settings;

	private Vector3 velocity;
	private Vector3 desiredVelocity;

	private Rigidbody body;

	private bool desiredJump;
	private int groundContactCount, steepContactCount;
	private bool OnGround => groundContactCount > 0; //get 
	private bool OnSteep => steepContactCount > 0;

	private float jumpPhase;

	private float minGroundDotProduct, minStairsDotProduct;

	private Vector3 contactNormal, steepNormal;


	//SURFACE CONTACT
	private int stepsSinceLastGrounded;
	private int stepsSinceLastJump;


	//Visually, this operation projects
	//one vector straight down to the other,
	//as if casting a shadow on it. In doing so,
	//you end up with a right triangle of which
	//the bottom side's length is the result of the dot product.
	//And if both vectors are unit length,
	//that's the cosine of their angle.
	private void OnValidate()
	{
		minGroundDotProduct = Mathf.Cos(_settings.maxGroundAngle * Mathf.Deg2Rad);
		minStairsDotProduct = Mathf.Cos(_settings.maxStairsAngle * Mathf.Deg2Rad);
	}

	private void Awake()
	{
		body = GetComponent<Rigidbody>();
		OnValidate();
	}

	//The part where we check for input and
	//set the desired velocity can remain in Update,
	//while the adjustment of the velocity should
	//move to a new FixedUpdate method.

	private void Update()
	{
		Vector2 playerInput;
		playerInput.x = Input.GetAxis("Horizontal");
		playerInput.y = Input.GetAxis("Vertical");
		playerInput = Vector2.ClampMagnitude(playerInput, 1f);

		desiredVelocity =
			new Vector3(playerInput.x, 0f, playerInput.y) * _settings.maxSpeed;

		desiredJump = Input.GetKeyDown(KeyCode.Space);

	}

    private void FixedUpdate()
    {
		UpdateState();
		AdjustVelocity();


		if (desiredJump)
		{
			desiredJump = false;
			Jump();
		}

		body.velocity = velocity;
		ClearState();

	}

	private void AdjustVelocity()
    {
		Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
		Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

		float currentX = Vector3.Dot(velocity, xAxis);
		float currentZ = Vector3.Dot(velocity, zAxis);

		float acceleration = OnGround ? _settings.maxAcceleration : _settings.maxAirAcceleration;
		float maxSpeedChange = acceleration * Time.deltaTime;

		float newX = 
			Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
		float newZ = 
			Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

		velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);

	}
	private Vector3 ProjectOnContactPlane(Vector3 vector)
	{
		return vector - contactNormal * Vector3.Dot(vector, contactNormal);
	}

	private void UpdateState()
    {
		stepsSinceLastGrounded++;
		stepsSinceLastJump++;
		velocity = body.velocity;
        if (OnGround || SnapToGround() || CheckSteepContacts())
        {
			stepsSinceLastGrounded = 0;
			jumpPhase = 0;

			if(groundContactCount > 1) 
			{ 
				contactNormal.Normalize(); 
			}
        }
        else
        {
			contactNormal = Vector3.up;
        }
    }

    //Vy = (-2gh)^(1/2), g : gravity, h : desired height
    private void Jump()
	{
		Vector3 jumpDirection;
		if (OnGround) jumpDirection = contactNormal;
		else if (OnSteep)
		{
			jumpDirection = steepNormal;
			jumpPhase = 0;
		}
		else if (_settings.maxAirJumps > 0 && jumpPhase <= _settings.maxAirJumps)
		{
			if (jumpPhase == 0) jumpPhase = 1;
			jumpDirection = contactNormal;
		}
		else return;
		
		stepsSinceLastJump = 0;

		if (stepsSinceLastJump > 1) jumpPhase = 0;

		float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * _settings.jumpHeight);

		jumpDirection = (jumpDirection + Vector3.up).normalized;

		float alignedSpeed = Vector3.Dot(velocity,
			jumpDirection);

		//relative correction while preventing slowing down
		if(alignedSpeed > 0)
        {
			jumpSpeed = Mathf.Max(jumpSpeed - 
				alignedSpeed, 0f);
        }

		velocity += jumpDirection * jumpSpeed;

		
	}

	private void ClearState()
    {
		groundContactCount = steepContactCount = 0;
		contactNormal = steepNormal = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
		EvaluateCollision(collision);
	}

	private void OnCollisionStay(Collision collision)
	{
		EvaluateCollision(collision);
	}

	private void EvaluateCollision(Collision collision) 
	{
		float minDot = GetMinDot(collision.gameObject.layer);
		for (int i = 0; i < collision.contactCount; i++)
		{
			Vector3 normal = collision.GetContact(i).normal;

			if(normal.y >= minDot)
            {
				groundContactCount += 1;
				contactNormal += normal;
            }else if (normal.y > -0.01f)
            {
				steepContactCount++;
				steepNormal += normal;
            }
		}
	}

    private bool SnapToGround()
    {
		if (stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
        {
			return false;
        }

		float speed = velocity.magnitude;
		if (speed > _settings.maxSnapSpeed)
        {
			return false;
        }

		if (!Physics.Raycast(body.position, 
			Vector3.down, out RaycastHit hit, 
			_settings.probeDistance, _settings.probeMask))
        {
			return false;
        }

		if (hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
        {
			return false;
        }

		groundContactCount = 1;
		contactNormal = hit.normal;
		float dot = Vector3.Dot(velocity, hit.normal);
		if (dot > 0f)
		{
			velocity = (velocity - hit.normal * dot).normalized * speed;
		}
		return true	;
    }

	private float GetMinDot (int layer)
    {
		return (_settings.stairsMask & (1 << layer)) == 0 ?
			minGroundDotProduct : minStairsDotProduct;
    }

	private bool CheckSteepContacts()
    {
		if (steepContactCount > 1)
        {
			steepNormal.Normalize();
			if (steepNormal.y >= minGroundDotProduct)
            {
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
            }
        }
        return false;

    }
}
