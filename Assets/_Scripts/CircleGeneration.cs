using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleGeneration : MonoBehaviour {

    [Header("Grid Parameters")]
    //Grid generation
    ParticleSystem _ps;
    ParticleSystem.Particle[] particles;
    Vector2 center;
    public float startingRadius = 0f;
    public float radiusOffset = 1.5f;
    public int startingParticles = 1;
    public int numRings = 5;
    public float resolution;
    int numParticlesInRing;
    float radius;

    int numParticles = 1;

    [Space(2)]
    [Header("Perlin Values")]
    //Perlin values
    public float perlinValue = 1.1f;
    public float perlinSpeed = 0.1f;
    public float distortionScale = 1.5f;
    float[] distortionHeight;

    [Space(2)]
    [Header("Hookes Law")]
    //Hookes Law
    public float k = 0.01f;
    float force;
    public Vector3 upWardsForce;
    Vector3 velocity;
    public float drag;

    //Noise Reaction
    public AudioAnalysis Analysis;
    public AnimationCurve AnimCurve;
    public Vector2 minMaxSpeed;
    public float threshold;
    public float waitTime;
    public float offsetScale;

    bool isMoving;
    float posOnAnimCurve;

    float _timer;
    public float movementSpeed = 0.01f;

    void OnEnable() {
        _ps = GetComponent<ParticleSystem>();
        center.x = transform.localPosition.x;
        center.y = transform.localPosition.z;
        numParticlesInRing = startingParticles;
        distortionHeight = new float[numRings];
        radius = startingRadius;

        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();

        //spawn first particle in center
        Vector3 position;

        for (int i = 0; i < numRings; i++) {
            for (int j = 0; j < numParticlesInRing; j++) {
                float theta = (Mathf.PI * 2) / numParticlesInRing;

                position.x = center.x + radius * Mathf.Cos(theta * j);
                position.z = center.y + radius * Mathf.Sin(theta * j);
                position.y = 0;

                ep.position = position;
                _ps.Emit(ep, 1);
            }
            radius += radiusOffset;
            numParticlesInRing = (int)((Mathf.PI * 2 * radius) / resolution);       //num particles in each circle is relative to circumference
            numParticles += numParticlesInRing;
        }
        particles = new ParticleSystem.Particle[numParticles];
        _ps.GetParticles(particles);
        GetTargetHeights();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            velocity += upWardsForce;
        }

        AddForceOnBeat();

        _ps.SetParticles(particles);
    }

    IEnumerator MoveParticles() {
        radius = 0;
        numParticlesInRing = 1;
        int particleIndex = 0;

        for (int i = 0; i < numRings; i++) {
            StartCoroutine(MoveRingOfParticles(numParticlesInRing, particleIndex, i));
           
            particleIndex += numParticlesInRing;
            radius += radiusOffset;
            numParticlesInRing = (int)((Mathf.PI * 2 * radius) / resolution);
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator MoveRingOfParticles(int numParticlesInRing, int particleIndex, float ringNum) {
        float posOnAnimCurve = 0;

        while(posOnAnimCurve < 1f) {
            float particleHeight = AnimCurve.Evaluate(Mathf.Clamp01(posOnAnimCurve)) * offsetScale * (1f - (ringNum/numRings));
            for (int j = 0; j < numParticlesInRing; j++) { 
                Vector3 position = new Vector3(particles[particleIndex + j].position.x, particleHeight, particles[particleIndex + j].position.z);
                particles[particleIndex + j].position = position;
            }
            posOnAnimCurve += movementSpeed;
            yield return null;
        }
        yield return new WaitForSeconds(waitTime);
        isMoving = false;
    }

    IEnumerator MusicReact() {
        float beatStrength = Analysis.bandBuffer[Analysis.FocusBand];
        float offSetHeight = Mathf.Lerp(minMaxSpeed.x, minMaxSpeed.y, AnimCurve.Evaluate(beatStrength));
        float offSetScale = 3f;
        offSetHeight *= offSetScale;

        int particleIndex = 0;
        radius = 0;
        numParticlesInRing = 1;

        for (int i = 0; i < numRings; i++) {
            for (int j = 0; j < numParticlesInRing; j++) {
                float offSetHeightAdjust = offSetHeight * (float)(numRings - i) / (numRings);
                Vector3 position = new Vector3(particles[particleIndex].position.x, offSetHeightAdjust, particles[particleIndex].position.z);
                particles[particleIndex].position = position;
                particleIndex++;
                yield return new WaitForSeconds(waitTime);
            }
            radius += radiusOffset;
            numParticlesInRing = (int)((Mathf.PI * 2 * radius) / resolution);
        }
    }

    void AddForceOnBeat() {
        float beatStrength = Analysis.bandBuffer[Analysis.FocusBand];

        if(beatStrength > threshold && !isMoving) {
            isMoving = true;
            StartCoroutine(MoveParticles());
        }
    }

    IEnumerator CalculateSpringForce() {
        float displacement = particles[0].position.y - 0;
        float force = -k * displacement;
        Vector3 springForce = new Vector3(0, force, 0);
        velocity += springForce;
        velocity *= drag;

        int particleIndex = 0;
        radius = 0;
        numParticlesInRing = 1;

        //Hooke's Law : F = -K * displacement
        for (int i = 0; i < numRings; i++) {
            for (int j = 0; j < numParticlesInRing; j++) {
                particles[particleIndex].position += (velocity * ((float)(numRings - i)/(numRings)));
                particleIndex++;
                print(particleIndex);
                yield return new WaitForSeconds(waitTime);
            }
            radius += radiusOffset;
            numParticlesInRing = (int)((Mathf.PI * 2 * radius) / resolution);
        }
    }
    
    void WaveParticles() {
        _timer = _timer + Time.deltaTime ;
        print(_timer);
        int distortionIndex = 0;
        int particleIndex = 0;
        radius = startingRadius;
        float newHeight = Mathf.Lerp(particles[particleIndex].position.y, distortionHeight[distortionIndex], _timer); ;

        numParticlesInRing = startingParticles;

        //first case
        particles[particleIndex].position = new Vector3(particles[particleIndex].position.x, newHeight, particles[particleIndex].position.z);
        particleIndex++;
        distortionIndex++;

        for (int i = 1; i < numRings; i++) {
            newHeight = Mathf.Lerp(particles[particleIndex].position.y, distortionHeight[distortionIndex], _timer);
            for (int j = 0; j < numParticlesInRing; j++) {
                particles[particleIndex].position = new Vector3(particles[particleIndex].position.x, newHeight, particles[particleIndex].position.z);
                particleIndex++;   
            }
            radius += radiusOffset;
            numParticlesInRing = (int)((Mathf.PI * 2 * radius) / resolution);
            distortionIndex++;
        }
        _ps.SetParticles(particles);
    }

    void GetTargetHeights() {
        for (int i = 0; i < numRings; i++) {
            float height = Mathf.PerlinNoise(perlinValue, 0.0f);
            height = Remap(height, 0, 1, -1, 1);
            distortionHeight[i] = height * distortionScale;            
            perlinValue += perlinSpeed;
        }
    }

    float Remap(float originalValue, float fromA, float toA, float fromB, float toB) {
        float normal = Mathf.InverseLerp(fromA, toA, originalValue);
        float newValue = Mathf.Lerp(fromB, toB, normal);
        return newValue;
    }



}
