using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Boid prefab;
    public BoidSettings settings;

    private void Awake() {
        for (int i = 0; i < settings.numBoids; i++) {
            Boid boid = Instantiate(prefab);
        }
    }
}
