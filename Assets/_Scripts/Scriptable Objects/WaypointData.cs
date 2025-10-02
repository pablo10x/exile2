using UnityEngine;
using System;
using System.Collections.Generic;

public enum WaypointType
{
    npcPath,
    road,
    RestingPlace,
    TrafficLane,
    Intersection
}

[Serializable]
public class Waypointx
{
    public Vector3 position;
    public WaypointType type;
    public List<int> connectedWaypoints = new List<int>();
    public string roadName;

    public Waypointx(Vector3 pos, WaypointType t, string road = "")
    {
        position = pos;
        type = t;
        roadName = road;
    }
}

[CreateAssetMenu(fileName = "New Waypoint Data", menuName = "Waypoint Data")]
public class WaypointData : ScriptableObject
{
    public List<Waypointx> waypoints = new List<Waypointx>();
    public bool isLooped = false;
}
