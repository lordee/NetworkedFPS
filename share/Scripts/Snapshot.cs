using Godot;
using System;
using System.Collections.Generic;

public class Snapshot
{
    public int SnapNum;
    public List<PlayerSnap> PlayerSnap = new List<PlayerSnap>();
}

public class PlayerSnap
{
    public string NodeName;
    public List<PlayerCmd> CmdQueue;
    public Vector3 Origin;
    public Vector3 Velocity;
    public Vector3 Rotation;
}