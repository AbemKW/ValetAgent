using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CarAgent : Agent
{
    private ControlInterface controlInterface;
    public GameObject SpawnPoints;
    public GameObject TargetSpot;
    public GameObject ParkingSpots;
    public GameObject ObstacleCarPrefab;
    public GameObject TargetGoal;

    private PrometeoCarController carController;
    private Rigidbody rb;
    private List<GameObject> obstacleCars = new List<GameObject>();

    private float lastDistance;

    void Start()
    {
        controlInterface = GetComponent<ControlInterface>();
        carController = GetComponent<PrometeoCarController>();
        rb = GetComponent<Rigidbody>();
        int maxObstacles = (ParkingSpots.GetComponentsInChildren<Transform>().Length - 1) - 10;
        for (int i = 0; i < maxObstacles; i++)
        {
            GameObject obstacleCar = Instantiate(ObstacleCarPrefab, Vector3.one * 500, Quaternion.identity, ParkingSpots.transform);
            obstacleCars.Add(obstacleCar);
        }
        lastDistance = Vector3.Distance(transform.position, TargetSpot.transform.position);
    }

    public override void OnEpisodeBegin()
    {
        MaxStep = 20000; // End episode after 10000 steps
        controlInterface.Gas = false;
        controlInterface.Brake = false;
        controlInterface.Left = false;
        controlInterface.Right = false;
        controlInterface.HandBrake = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        var spawnPointTransforms = SpawnPoints.GetComponentsInChildren<Transform>().Where(t => t != SpawnPoints.transform).ToArray();
        int randomIndex = Random.Range(0, spawnPointTransforms.Length);
        Transform spawnPoint = spawnPointTransforms[randomIndex];
        transform.SetPositionAndRotation(spawnPoint.position + new Vector3(0, 0.5f, 0), Quaternion.Euler(0, Random.Range(0f, 360f), 0));


        ResetTarget();
        lastDistance = Vector3.Distance(transform.position, TargetGoal.transform.position);

        ResetObstacles();

    }

    private void ResetObstacles()
    {
        var targetSpot = ParkingSpots.GetComponentsInChildren<ParkingSpot>();
        var availableSpots = targetSpot
            .Where(t => t.transform.position != TargetSpot.transform.position && Vector3.Distance(t.transform.position,TargetSpot.transform.position) > 2f)
            .OrderBy(t => Random.value)
            .ToArray();
        for (int i = 0; i < obstacleCars.Count; i++)
        {
            if (i < availableSpots.Length)
            {
                Transform obstacleSpot = availableSpots[i].transform;
                obstacleCars[i].SetActive(true);
                obstacleCars[i].transform.SetPositionAndRotation(obstacleSpot.position, obstacleSpot.rotation);
            }
            else
            {
                obstacleCars[i].SetActive(false);
            }
        }
    }

    void ResetTarget()
    {
        var targetSpotTransforms = ParkingSpots.GetComponentsInChildren<ParkingSpot>();
        Transform selectedTargetSpot = targetSpotTransforms[Random.Range(0, targetSpotTransforms.Length)].transform;
        TargetSpot.transform.SetPositionAndRotation(selectedTargetSpot.position, selectedTargetSpot.rotation);
    }
    Vector3 directionToTarget => (TargetGoal.transform.position - transform.position).normalized;
    float distanceToTarget => Vector3.Distance(transform.position, TargetGoal.transform.position);
    float angleToTarget => Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(distanceToTarget / 140);
        sensor.AddObservation(angleToTarget / 180f);
        sensor.AddObservation(rb.linearVelocity.magnitude / 20f);
        sensor.AddObservation(Vector3.Dot(rb.linearVelocity.normalized, transform.forward));
        sensor.AddObservation(rb.angularVelocity.y / 10f); // normalize

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        SetInputs(actions);
        // --- Reward 1: Distance progress (difference based) ---
        float distanceDelta = lastDistance - distanceToTarget;
        AddReward(distanceDelta * 0.05f); // reward for moving closer
        lastDistance = distanceToTarget;

        // --- Reward 2: Heading alignment ---
        float alignment = Vector3.Dot(transform.forward, directionToTarget);
        AddReward(alignment * 0.1f * Time.fixedDeltaTime);

        // --- Reward 3: Velocity alignment ---
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float velocityAlignment = Vector3.Dot(rb.linearVelocity.normalized, transform.forward);
            AddReward(velocityAlignment * 0.002f * Time.fixedDeltaTime);
        }

        float angularVelPenalty = Mathf.Clamp01(Mathf.Abs(rb.angularVelocity.y) / 5f);
        AddReward(-angularVelPenalty * 0.005f);

        // --- Penalty 2: Step penalty (encourages efficiency) ---
        AddReward(-0.001f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == TargetGoal)
        {
            float alignment = Vector3.Dot(transform.forward, directionToTarget);
            float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
            float alignmentBonus = alignment * 10f;
            float misalignmentPenalty = Mathf.Max(0, 1 - alignment) * 5f;

            // Aligned with car properties
            if (alignment > 0.98f && carController.carSpeed < 0.3f && Mathf.Abs(rb.angularVelocity.y) < 0.05f)
            {
                TargetSpot.GetComponent<Renderer>().material.color = Color.green;
                Debug.Log($"Target reached with alignment bonus: {alignmentBonus}, Angle: {angleToTarget:F2}, Speed: {carController.carSpeed:F2}, AngularVel: {rb.angularVelocity.y:F2}");
                SetReward(10f + alignmentBonus - misalignmentPenalty);
                EndEpisode();
            }
            else
            {
                Debug.LogWarning($"Misaligned or moving: Alignment: {alignment:F2}, Angle: {angleToTarget:F2}, Speed: {carController.carSpeed:F2}, AngularVel: {rb.angularVelocity.y:F2}, Penalty: {misalignmentPenalty:F2}");
                SetReward(10f + alignmentBonus - misalignmentPenalty);
                EndEpisode();
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Collision with obstacle, ending episode.");
            SetReward(-3f);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Curb"))
        {
            Debug.Log("Collision with curb, ending episode.");
            SetReward(-0.5f);
            EndEpisode();
        }
    }

    void SetInputs(ActionBuffers actions)
    {
        var output = actions.DiscreteActions;
        switch (output[0])
        {
            case 0: // Gas
                controlInterface.Gas = true;
                controlInterface.Brake = false;
                break;
            case 1: // Brake
                controlInterface.Gas = false;
                controlInterface.Brake = true;
                break;
            default:
                controlInterface.Gas = false;
                controlInterface.Brake = false;
                break;
        }

        switch (output[1])
        {
            case 0: // Left
                controlInterface.Left = true;
                controlInterface.Right = false;
                break;
            case 1: // Right
                controlInterface.Left = false;
                controlInterface.Right = true;
                break;
            default:
                controlInterface.Left = false;
                controlInterface.Right = false;
                break;
        }

        controlInterface.HandBrake = output[2] == 1;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var output = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W))
        {
            output[0] = 0; // Gas
        }
        else if (Input.GetKey(KeyCode.S))
        {
            output[0] = 1; // Brake
        }
        else
        {
            output[0] = 2; // No action
        }
        if (Input.GetKey(KeyCode.A))
        {
            output[1] = 0; // Left
        }
        else if (Input.GetKey(KeyCode.D))
        {
            output[1] = 1; // Right
        }
        else
        {
            output[1] = 2; // No action
        }
        if (Input.GetKey(KeyCode.Space))
        {
            output[2] = 1; // HandBrake
        }
        else
        {
            output[2] = 0; // No handbrake
        }
    }
}