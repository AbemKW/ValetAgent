using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.Entities;
using System.Linq;

public class CarAgent : Agent
{
    private ControlInterface controlInterface;
    public GameObject SpawnPoints;
    public GameObject TargetSpot;
    public GameObject ParkingSpots;
    public GameObject ObstacleCarPrefab;

    List<GameObject> obstacleCars = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        controlInterface = GetComponent<ControlInterface>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public override void OnEpisodeBegin()
    {
        var spawnPointTransforms = SpawnPoints.GetComponentsInChildren<Transform>();
        // Randomly select a spawn point
        int randomIndex = Random.Range(1, spawnPointTransforms.Length); // Start from 1 to skip the parent transform
        Transform spawnPoint = spawnPointTransforms[randomIndex];
        // Set the position of the car to the selected spawn point
        transform.position = spawnPoint.position + new Vector3(0, 0.5f, 0); // Adjust height to avoid collision with ground
        // Reset the rotation of the car to be randomly facing a direction
        transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        // Randomly select a target spot
        var targetSpotTransforms = ParkingSpots.GetComponentsInChildren<Transform>();
        int targetIndex = Random.Range(1, targetSpotTransforms.Length); // Start from 1 to skip the parent transform
        Transform targetSpot = targetSpotTransforms[targetIndex];
        // Set the target position
        TargetSpot.transform.position = targetSpot.position;
        TargetSpot.transform.rotation = targetSpot.rotation;

        // Clear previous obstacle cars
        foreach (var obstacle in obstacleCars)
        {
            Destroy(obstacle);
        }
        obstacleCars.Clear();
        // Spawn new obstacle cars everywhere except the target spot
        // After selecting targetIndex:
        for (int i = 1; i < targetSpotTransforms.Length; i++)
        {
            if (i == targetIndex) continue; // Skip the target spot
            Transform spot = targetSpotTransforms[i];
            GameObject obstacleCar = Instantiate(ObstacleCarPrefab, spot.position, spot.rotation, ParkingSpots.transform);
            obstacleCars.Add(obstacleCar);
        }
    }
    float distanceToTarget => Mathf.Min(Vector3.Distance(transform.position, TargetSpot.transform.position), 5f) / 5f; // Normalize distance to a value between 0 and 1
    float angleToTarget => Vector3.Angle(transform.forward, TargetSpot.transform.forward) / 180f; // Normalize angle to a value between 0 and 1
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(distanceToTarget);
        sensor.AddObservation(angleToTarget);
    }
    public override void OnActionReceived(ActionBuffers actions)
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

        AddReward(-0.01f * distanceToTarget); // Penalize for distance to target
        if (distanceToTarget < 1f)
        {
            AddReward(10f); // Reward for reaching the target
            Debug.Log("Reached target, ending episode.");
            EndEpisode();
        }
        // Temporarily DIsabled, First Learn Path Following, THen Parking
        if (false && controlInterface.HandBrake && distanceToTarget < 1f)
        {
            float finalReward = Mathf.Pow(1 - Mathf.Abs(angleToTarget), 4) + (1 - distanceToTarget); // Combine rewards, emphasizing alignment
            finalReward *= 15f; // Scale the reward
            Debug.Log($"Final Reward: {finalReward * 15}");
            SetReward(finalReward);
            EndEpisode();
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("Collision with obstacle, ending episode.");
            SetReward(-1f); // Penalize for collision
            EndEpisode();
        }
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
