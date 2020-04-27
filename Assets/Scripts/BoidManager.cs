using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour {
    
    const int threadGroupSize = 2048;
    public BoidSettings settings;
    public ComputeShader compute;
    Boid[] boids;
	int numBoids;
	
	float C;
	float correlationLength;
	Vector2 avgVelocity, polarisation;
	float maxDistance = 0f;
	int numConsecutiveFlocked = 0;
	string[] values = new string[100];
	int stringsIndex = 0;

    void Start() {
        boids = FindObjectsOfType<Boid>();
        foreach (Boid b in boids) {
            b.Initialize(settings);
		}
    }

    void Update() {
		// calculate the world locations of the mouse clicks, pass them to the boids
		if (UnityEngine.Input.GetMouseButton(0)) { 
			Boid.mouseClickPos = new Vector2(((Input.mousePosition.x / Screen.width) * settings.screenWidth) - settings.screenWidth / 2,
										     ((Input.mousePosition.y / Screen.height) * settings.screenHeight) - settings.screenHeight / 2);
		}
		if (!UnityEngine.Input.GetMouseButton(0)) {
			Boid.mouseClickPos = Vector2.zero;
		}
		if (UnityEngine.Input.GetMouseButton(1)) { 
			Boid.mouseClick2Pos = new Vector2(((Input.mousePosition.x / Screen.width) * settings.screenWidth) - settings.screenWidth / 2,
										      ((Input.mousePosition.y / Screen.height) * settings.screenHeight) - settings.screenHeight / 2);
		}
		if (!UnityEngine.Input.GetMouseButton(1)) {
			Boid.mouseClick2Pos = Vector2.zero;
		}
		
        if (boids != null) {

            numBoids = boids.Length;
            var boidData = new BoidData[numBoids];

            // update buffer boidData from boids current positions
            for (int i = 0; i < boids.Length; i++) {
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
            }

            var boidBuffer = new ComputeBuffer(numBoids, BoidData.Size);
            boidBuffer.SetData(boidData);

            compute.SetBuffer(0, "boids", boidBuffer);
            compute.SetInt("numBoids", boids.Length);
            compute.SetFloat("viewRadius", settings.perceptionRadius);
            compute.SetFloat("avoidRadius", settings.avoidanceRadius);
			compute.SetFloat("minFov", settings.minFov * Mathf.PI / 180);
			compute.SetFloat("maxFov", settings.maxFov * Mathf.PI / 180);
			compute.SetInt("screenWidth", settings.screenWidth);
			compute.SetInt("screenHeight", settings.screenHeight);
			compute.SetInt("maxNumNeighbours", settings.NNearestNeighbours);

            int threadGroups = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
            compute.Dispatch(0, threadGroups, 1, 1);

            boidBuffer.GetData(boidData);

			// get the data from the compute shader about each boid's neighbourhood
            for (int i = 0; i < boids.Length; i++) {
                boids[i].avgFlockHeading = boidData[i].flockHeading;
                boids[i].centreOfFlockmates = boidData[i].flockCentre;
                boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                boids[i].numFlockmates = boidData[i].numFlockmates;

                boids[i].UpdateBoid();
            }

            boidBuffer.Release();
			
			// do scale free calculations
			// only if the birds have formed a cohesive flock
			// all velocities pointing roughly same direction, magnitude of polarisation >= 0.975
			polarisation = Vector2.zero;
			for (int i = 0; i < numBoids; i++) {
				polarisation += (boids[i].velocity / boids[i].velocity.magnitude);
			}
			polarisation /= numBoids;
			
			// require 10 consecutive iterations where the birds are in a flock to reduce noise
			if (polarisation.magnitude >= settings.polarisationLimit && numConsecutiveFlocked >= 10) {
				
				// calculate average velocity of flock, needed for correlation calculation
				avgVelocity = Vector2.zero;
				for (int i = 0; i < numBoids; i++) {
					avgVelocity += boids[i].velocity;
				}
				avgVelocity /= numBoids;
				
				// calculate correlation length - length at which C >= 0
				correlationLength = 0.5f;
				do {
					correlationLength += 0.05f;
					C = correlation(correlationLength, 0.1f);
				} while (C > settings.correlationLimit && correlationLength < 30);
				
				// get scale of flock - max distance between any two birds
				maxDistance = 0f;
				for (int i = 0; i < numBoids; i++) {
					for (int j = 0; j < numBoids; j++) {
						if (Vector2.Distance(boids[i].position, boids[j].position) > maxDistance) {
							maxDistance = Vector2.Distance(boids[i].position, boids[j].position);
						}
					}
				}
				
				// log the flock scale against correlation length
				// ignore is the distance is too large, as it indicates flock is split across boundary / two sub-flocks
				if (maxDistance < settings.screenHeight / 2) {
					values[stringsIndex] = maxDistance + "," + correlationLength;
					stringsIndex += 1;
					
					if (stringsIndex >= 100) {
						System.IO.File.AppendAllLines(@settings.outputLocation + "\\correlations.txt", values);
						stringsIndex = 0;
					}
				}
			} else if (polarisation.magnitude >= settings.polarisationLimit) {
				numConsecutiveFlocked += 1;
			} else {
				numConsecutiveFlocked = 0;
			}
        }
    }
	
	private float correlation(float d, float epsilon) {
		float denominator = 0f, numerator = 0f, distance;
		int count = 0;
		
		// calculate denominator
		for (int i = 0; i < numBoids; i++) {
			denominator += Vector2.Dot(boids[i].velocity - avgVelocity, boids[i].velocity - avgVelocity);
		}
		denominator /= numBoids;
		
		// calculate numerator
		for (int i = 0; i < numBoids; i++) {
			for (int j = 0; j < numBoids; j++) {
				distance = Vector2.Distance(boids[i].position, boids[j].position);
				// dirac delta function
				if (Mathf.Abs(distance - d) <= epsilon) {
					count += 1;
					numerator += Vector2.Dot(boids[i].velocity - avgVelocity, boids[j].velocity - avgVelocity);
				}
			}
		}
		numerator /= count;
		return numerator / denominator;
	}

    public struct BoidData {
        public Vector2 position;
        public Vector2 direction;

        public Vector2 flockHeading;
        public Vector2 flockCentre;
        public Vector2 avoidanceHeading;
        public int numFlockmates;

        public static int Size {
            get {
                return sizeof(float) * 2 * 5 + sizeof(int);
            }
        }
    }
}
