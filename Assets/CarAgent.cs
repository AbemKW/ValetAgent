using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CarAgent : Agent
{
    private ControlInterface controlInterface;
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
        Vector3 startPosition = new Vector3(Random.Range(-50f, 57f), 0.51f, 42);
        transform.position = startPosition;

        Vector3 startRotation = new Vector3(0, Random.Range(0f, 360f), 0);
        transform.rotation = Quaternion.Euler(startRotation);
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.rotation.eulerAngles);
        // velocity
        sensor.AddObservation(GetComponent<Rigidbody>().velocity);
        sensor.AddObservation(transform.forward);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        var actionsDiscrete = actions.DiscreteActions;

        controlInterface.Gas = actionsDiscrete[0] == 1;
        controlInterface.Brake = actionsDiscrete[0] == 1;
        controlInterface.Left = actionsDiscrete[1] == 1;    
        controlInterface.Right = actionsDiscrete[1] == 1;
        controlInterface.HandBrake = actionsDiscrete[2] == 1;

    }
}
