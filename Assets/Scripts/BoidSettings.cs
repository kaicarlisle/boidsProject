using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BoidSettings : ScriptableObject
{
    // Settings
	[Header("Basic settings")]
    [Range(0, 1000)]
    public int numBoids = 800;
	[HideInInspector]
	[Range(1,10)]
    public float spawnRadius = 10;
	[Range(0,5)]
    public float minSpeed = 2;
	[Range(5,20)]
    public float maxSpeed = 6;
	[Range(0,5)]
    public float maxSteerForce = 3;
	[Range(0,1)]
    public float alignWeight = 1;
	[Range(0,1)]
    public float cohesionWeight = 1;
	[Range(0,1)]
    public float seperateWeight = 2;
	
	[Space]
	[Header("Settings for visual neighbour system")]
	// using visual neighbour system
	[Range(0,30)]
    public float perceptionRadius = 3f;
	[Range(0,10)]
    public float avoidanceRadius = 0.5f;
	[Range(0,180)]
	public float minFov = 65;
	[Range(0,180)]
	public float maxFov = 115;
	
	[Space]
	[Header("Settings for topological neighbour system")]
	// using topological neighbour system
	[Range(1,50)]
	public int NNearestNeighbours = 7;
	
	
	//[HideInInspector]
	public int screenWidth = 58;
	//[HideInInspector]
	public int screenHeight = 32;
}
