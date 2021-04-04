using Godot;
using System.Collections.Generic;

public class PlayerCmd
{
    public int snapshot;
    public int playerID;
    public float move_forward;
    public float move_right;
    public float move_up;
    public Basis basis;
    public float cam_angle; // FIXME - basis....
    public int attack;
    public List<float> impulses = new List<float>();
}