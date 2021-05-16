using System;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{

	[SerializeField, Range(0f, 100f)]
	private float maxSpeed = 10f;

	[SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f, maxAirAcceleration = 1f;

	[SerializeField, Range(0f, 10f)]
	private float jumpHeight = 2f;

	[SerializeField, Range(0, 5)]
	int maxAirJumps = 0;

	[SerializeField, Range(0f, 90f)]
	float maxGroundAngle = 25f;



	private Vector3 velocity, desiredVelocity;


	private Rigidbody body;


	private bool desiredJump;
	private int groundContactCount;
	private bool OnGround => groundContactCount > 0; //get 


	private float jumpPhase;


	private float minGroundDotProduct;

	private Vector3 contactNormal;


	//Visually, this operation projects
	//one vector straight down to the other,
	//as if casting a shadow on it. In doing so,
	//you end up with a right triangle of which
	//the bottom side's length is the result of the dot product.
	//And if both vectors are unit length,
	//that's the cosine of their angle.
	private void OnValidate()
	{
		minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
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
			new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

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

		float acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
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
		velocity = body.velocity;
        if (OnGround)
        {
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
		
		if (OnGround || jumpPhase < maxAirJumps)
        {
			jumpPhase += 1;
			float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
			float alignedSpeed = Vector3.Dot(velocity, contactNormal);

			//relative correction while preventing slowing down
			if(alignedSpeed > 0)
            {
				jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
            }

			velocity += contactNormal * jumpSpeed;

		}
	}

	private void ClearState()
    {
		groundContactCount = 0;
		contactNormal = Vector3.zero;
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
		for (int i = 0; i < collision.contactCount; i++)
		{
			Vector3 normal = collision.GetContact(i).normal;

			if(normal.y >= minGroundDotProduct)
            {
				groundContactCount += 1;
				contactNormal += normal;
            }
		}
	}




}
