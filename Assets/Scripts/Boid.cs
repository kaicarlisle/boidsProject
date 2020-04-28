using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    BoidSettings settings;

    // State
    public Vector2 position;
    public Vector2 velocity;
    public Vector2 acceleration;
    public Vector2 forward;

    // Info gathered about neighbours through BoidManager
    public Vector2 avgFlockHeading;
    public Vector2 avgAvoidanceHeading;
    public Vector2 centreOfFlockmates;
    public int numFlockmates;
	public static Vector2 mouseClickPos, mouseClick2Pos;

    // Material
    Material material;

    void Awake() {
        material = transform.GetComponentInChildren<MeshRenderer>().material;
        material.color = Color.red;
    }

    public void Initialize(BoidSettings settings) {
        this.settings = settings;

        position = Random.insideUnitCircle * settings.spawnRadius;
        forward = Random.insideUnitCircle;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = forward * startSpeed;
    }

    public void UpdateBoid() {
        // Update the position and forward for this boid, based on the info
        // collected about all other(/neighbouring) boids in BoidManager

        // acceleration resets each frame
        acceleration = Vector2.zero;

        if (numFlockmates != 0) {
            Vector2 offsetToCentreOfFlockmates = centreOfFlockmates - position;

            var alignmentForce = SteerTowards(avgFlockHeading) * settings.alignWeight;
            var cohesionForce = SteerTowards(offsetToCentreOfFlockmates) * settings.cohesionWeight;
            var separationForce = SteerTowards(avgAvoidanceHeading) * settings.seperateWeight;

            acceleration += alignmentForce + cohesionForce + separationForce;
        }
		
		// add the mouse click forces
		if (mouseClickPos != Vector2.zero) {
			acceleration += SteerTowards(mouseClickPos - position) * 2;
		}
		if (mouseClick2Pos != Vector2.zero) {
			acceleration += SteerTowards(mouseClick2Pos - position) * -50;
		}
	
		if (!settings.wrapScreen) {
			// steer away from walls if about to collide
			// steer more strongly the closer you are to the wall
			if (position.x > settings.screenWidth/2 - 5 && velocity.x > 0) { 
				//right wall
				acceleration += (SteerTowards(Vector2.left) + SteerTowards(Vector2.Reflect(forward, Vector2.left))) * (5-(settings.screenWidth / 2 - position.x));
			}
			if (position.x < -settings.screenWidth/2 + 5 && velocity.x < 0) {
				// left wall
				acceleration += (SteerTowards(Vector2.right) + SteerTowards(Vector2.Reflect(forward, Vector2.right))) * (5+(-settings.screenWidth / 2 - position.x));
			}
			if (position.y > settings.screenHeight/2 - 5 && velocity.y > 0) {
				// top wall
				acceleration += (SteerTowards(Vector2.down) + SteerTowards(Vector2.Reflect(forward, Vector2.down))) * (5-(settings.screenHeight / 2 - position.y));
			}
			if (position.y < -settings.screenHeight/2 + 5 && velocity.y < 0) {
				// bottom wall
				acceleration += (SteerTowards(Vector2.up) + SteerTowards(Vector2.Reflect(forward, Vector2.up))) * (5+(-settings.screenHeight / 2 - position.y));
			}
		}
		
		// calculate and clamp the acceleration -> velocity -> position
        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector2 dir = velocity / speed;
        speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
        velocity = dir * speed;
		
        position += velocity * Time.deltaTime;
	
		// wrap any birds which went off screen so we don't lose any
		if (position.x >= settings.screenWidth / 2) {
			position.x = 1 - settings.screenWidth / 2;
		}
		if (position.x <= -settings.screenWidth / 2) {
			position.x = (settings.screenWidth / 2) - 1;
		}
		if (position.y >= settings.screenHeight / 2) {
			position.y = 1 - settings.screenHeight / 2;
		}
		if (position.y <= -settings.screenHeight / 2) {
			position.y = (settings.screenHeight / 2) - 1;
		}
        
		// update draw position on screen
        transform.position = position;
		
        forward = dir;        
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        transform.rotation = rotation;
    }

    Vector2 SteerTowards(Vector2 vector) {
        Vector2 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector2.ClampMagnitude(v, settings.maxSteerForce);
    }
}
