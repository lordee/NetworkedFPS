using Godot;
using System;
using System.Collections.Generic;

public class Player : Entity
{
    // Nodes
    static string _playerResource = Util.GetResourceString("Scenes/Player.tscn");
    public Body Body;
    public Client ClientOwner;
    
    public int NetworkID;
    
    public bool WishJump = false;
    public float MoveSpeed = 32;
    private float _moveScale = 1f;
    private float _airAcceleration = 2.0f;          // Air accel
    private float _airDecceleration = 2.0f;         // Deacceleration experienced when opposite strafing
    private float _sideStrafeAcceleration = 50.0f;  // How fast acceleration occurs to get up to sideStrafeSpeed
    private float _sideStrafeSpeed = 3.0f;          // What the max speed to generate when side strafing
    private float _jumpSpeed = 27.0f;                // The speed at which the character's up axis gains when hitting jump
    private float _maxStairAngle = 20f;
    private float _stairJumpHeight = 9F;

    public float CurrentHealth = 100;
    public float CurrentArmour = 0;
    public float TimeDead = 0;
    public State ServerState;
    public State PredictedState;
    public PSTATE PState;
    public List<PlayerCmd> pCmdQueue = new List<PlayerCmd>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public Player()
    {
        
    }

    static public Player Instance()
    {
        PackedScene ps = ResourceLoader.Load(_playerResource) as PackedScene;
        Player player = ps.Instance() as Player;

        return player;
    }

    public void Init(Client client)
    {
        ClientOwner = client;
        NetworkID = ClientOwner.NetworkID;
        Name = ClientOwner.NetworkID.ToString();

        Body = GetNodeOrNull("Body") as Body;
        Body.Init(this);
        Body.Acceleration = 14.0f;
        Body.Deceleration = 10.0f;
    }

    public void Frame(float delta)
    {
        Main.ScriptManager.PlayerPreFrame(this);

        PredictedState = ServerState;
        if (pCmdQueue.Count == 0)
        {
            pCmdQueue.Add(
                new PlayerCmd{
                    snapshot = 0,
                    playerID = NetworkID,
                    move_forward = 0,
                    move_right = 0,
                    move_up = 0,
                    aim = new Basis(),
                    cam_angle = 0,
                    rotation = PredictedState.Rotation,
                    attack = 0
                    }
                );
        }
        else
        {
            pCmdQueue.Sort((x,y) => x.snapshot.CompareTo(y.snapshot));
        }

        Transform t = Body.GlobalTransform;
        t.origin = PredictedState.Origin; // by this point it's a new serverstate
        Body.GlobalTransform = t;

        foreach(PlayerCmd pCmd in pCmdQueue)
        {
            if (pCmd.snapshot <= ClientOwner.LastSnapshot)
            {
                continue;
            }
            Body.Rotation = pCmd.rotation;

            ClientOwner.LastSnapshot = pCmd.snapshot;

            switch (PState)
            {
                case PSTATE.DEAD:
                    DeadProcess(pCmd, delta);
                    break;
                default:
                    DefaultProcess(pCmd, delta);
                    break;
            }
        }      

        Main.World.MoveEntity(this.Body, delta);
        

        SetServerState(PredictedState.Origin, PredictedState.Velocity, PredictedState.Rotation, CurrentHealth, CurrentArmour);
        TrimCmdQueue();
        
        Main.ScriptManager.PlayerPostFrame(this);
    }

    public void SetServerState(Vector3 org, Vector3 velo, Vector3 rot, float health, float armour)
    {
        ServerState.Origin = org;
        ServerState.Velocity = velo;
        ServerState.Rotation = rot;
        CurrentHealth = health;
        CurrentArmour = armour;
    }

    public void TrimCmdQueue()
    {
        if (pCmdQueue.Count > 0)
        {
            int count = (Main.World.ServerSnapshot > Main.World.LocalSnapshot) ? pCmdQueue.Count - 1 : pCmdQueue.Count - (Main.World.LocalSnapshot - Main.World.ServerSnapshot);
             
            for (int i = 0; i < count; i++)
            {
                pCmdQueue.RemoveAt(0);
            }
        }
    }

    private void DefaultProcess(PlayerCmd pCmd, float delta)
    {
        if (IsNetworkMaster())
        {
            int diff = Main.World.LocalSnapshot - pCmd.snapshot;
            if (diff < 0)
            {
                return;
            }
            Main.World.RewindPlayers(diff, delta);
        }

        if (pCmd.attack == 1)
        {
            Main.ScriptManager.PlayerAttack(this);
        }

        Main.World.FastForwardPlayers();
        
        if (Body.MoveType == MOVETYPE.STEP)
        {
            this.ProcessMovementCmd(PredictedState, pCmd, delta);
        }
    }

    private void ProcessMovementCmd(State predState, PlayerCmd pCmd, float delta)
    {
        Body.Velocity = predState.Velocity;

        // queue jump
        if (pCmd.move_up == 1 && !WishJump)
        {
            WishJump = true;
        }
        if (pCmd.move_up <= 0)
        {
            WishJump = false;
        }

        if (Body.TouchingGround || Body.OnLadder)
        {
            GroundMove(delta, pCmd);
        }
        else
        {
            AirMove(delta, pCmd);
        }
    }

    private void GroundMove(float delta, PlayerCmd pCmd)
    {
        Vector3 wishDir = new Vector3();

        float scale = CmdScale(pCmd);

        wishDir += pCmd.aim.x * pCmd.move_right;
        wishDir -= pCmd.aim.z * pCmd.move_forward;
        wishDir = wishDir.Normalized();
        Vector3 moveDirectionNorm = wishDir;

        float wishSpeed = wishDir.Length();
        wishSpeed *= MoveSpeed;
        Accelerate(wishDir, wishSpeed, Body.Deceleration, delta);
       
        if (Body.OnLadder)
        {
            if (pCmd.move_forward != 0f)
            {
                Body.Velocity.y = MoveSpeed * (pCmd.cam_angle / 90) * pCmd.move_forward;
            }
            else
            {
                Body.Velocity.y = 0;
            }
            if (pCmd.move_right == 0f)
            {
                Body.Velocity.x = 0;
                Body.Velocity.z = 0;
            }
        }

        // walk up stairs
        if (wishSpeed > 0 && Body.StairCatcher.IsColliding())
        {
            Vector3 col = Body.StairCatcher.GetCollisionNormal();
            float ang = Mathf.Rad2Deg(Mathf.Acos(col.Dot(Main.World.Up)));
            if (ang < _maxStairAngle)
            {
                Body.Velocity.y = _stairJumpHeight;
            }
        }

        if (WishJump && Body.IsOnFloor())
        {
            // FIXME - if we add jump speed velocity we enable trimping right?
            Body.Velocity.y = _jumpSpeed;
        }
    }

    private void AirMove(float delta, PlayerCmd pCmd)
    {
        Vector3 wishdir = new Vector3();
        
        float wishvel = _airAcceleration;
        float accel;
        
        float scale = CmdScale(pCmd);

        wishdir += pCmd.aim.x * pCmd.move_right;
        wishdir -= pCmd.aim.z * pCmd.move_forward;

        float wishspeed = wishdir.Length();
        wishspeed *= MoveSpeed;

        wishdir = wishdir.Normalized();
        Vector3 moveDirectionNorm = wishdir;

        // CPM: Aircontrol
        float wishspeed2 = wishspeed;
        if (Body.Velocity.Dot(wishdir) < 0)
            accel = _airDecceleration;
        else
            accel = _airAcceleration;
        // If the player is ONLY strafing left or right
        if(pCmd.move_forward == 0 && pCmd.move_right != 0)
        {
            if(wishspeed > _sideStrafeSpeed)
            {
                wishspeed = _sideStrafeSpeed;
            }
                
            accel = _sideStrafeAcceleration;
        }

        Accelerate(wishdir, wishspeed, accel, delta);
        /*if(_airControl > 0)
        {
            AirControl(wishdir, wishspeed2, delta);
        }*/
        // !CPM: Aircontrol       
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel, float delta)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;
        
        currentspeed = Body.Velocity.Dot(wishdir);
        addspeed = wishspeed - currentspeed;
        if(addspeed <= 0)
            return;
        accelspeed = accel * delta * wishspeed;
        //if(accelspeed > addspeed)
         //   accelspeed = addspeed;
        Body.Velocity.x += accelspeed * wishdir.x;
        Body.Velocity.z += accelspeed * wishdir.z;
    }

    /*
    ============
    CmdScale
    Returns the scale factor to apply to cmd movements
    This allows the clients to use axial -127 to 127 values for all directions
    without getting a sqrt(2) distortion in speed.
    ============
    */
    
     private float CmdScale(PlayerCmd pCmd)
    {
        int max;
        float total;
        float scale;

        max = (int)Mathf.Abs(pCmd.move_forward);
        if(Mathf.Abs(pCmd.move_right) > max)
            max = (int)Mathf.Abs(pCmd.move_right);
        if(max <= 0)
            return 0;

        total = Mathf.Sqrt(pCmd.move_forward * pCmd.move_forward + pCmd.move_right * pCmd.move_right);
        scale = MoveSpeed * max / (_moveScale * total);

        return scale;
    }

    private void DeadProcess(PlayerCmd pCmd, float delta)
    {
        if (Body.TouchingGround)
        {
            TimeDead += delta;
        }

        if (TimeDead > .5)
        {
            if (pCmd.attack == 1 || pCmd.move_up == 1)
            {
                Main.ScriptManager.PlayerSpawn(this);
                TimeDead = 0;
            }
        }
    }
}
