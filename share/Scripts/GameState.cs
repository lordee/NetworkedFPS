using System.Collections.Generic;
using Godot;
using System;

[Serializable]
public class GameState
{
    public int SnapShotNumber = -1;
    public List<EntityState> EntityStates = new List<EntityState>();
    public bool Acked = false;
}