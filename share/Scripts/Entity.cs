using System.Collections.Generic;
using MoonSharp.Interpreter;
using Godot;
using System.Linq;
using System;

public class Entity
{
    [MoonSharpHidden]
    public UInt16 EntityID;
    
    [MoonSharpHidden]
    public EntityNode EntityNode;
    
    private Entity _owner;
    public Entity Owner {
        get {
            return _owner;
        }
        set {
            if (_owner != value)
            {
                if (_owner != null)
                {
                    EntityNode.KinematicBody.RemoveCollisionExceptionWith(_owner.EntityNode.KinematicBody);
                }
                
                EntityNode.KinematicBody.AddCollisionExceptionWith(value.EntityNode.KinematicBody);
                _owner = value;
            }
        }
    }

    public Vector3 ViewOffset
    {
        get { 
            if (EntityNode.Head != null)
            {
                return EntityNode.Head.Translation;
            }
            else
            {
                return new Vector3(0,0,0);
            }
            }
        set { 
            if (EntityNode.Head != null)
            {
                EntityNode.Head.Translation = value;
            }
        }
    }

    public Client ClientOwner;
    public bool OnLadder = false;
    public bool WishJump = false;
    public List<PlayerCmd> pCmdQueue = new List<PlayerCmd>();
    public float TimeDead = 0;
    // for lua state checks
    public int Attack = 0;
    private float _moveScale = 1f;
    private float _airAcceleration = 2.0f;          // Air accel
    private float _airDecceleration = 2.0f;         // Deacceleration experienced when opposite strafing
    private float _sideStrafeAcceleration = 50.0f;  // How fast acceleration occurs to get up to sideStrafeSpeed
    private float _sideStrafeSpeed = 3.0f;          // What the max speed to generate when side strafing
    private float _jumpSpeed = 27.0f;                // The speed at which the character's up axis gains when hitting jump
    private float _maxStairAngle = 20f;
    private float _stairJumpHeight = 9F;
    public State ServerState;
    public State PredictedState;
    
    public string NetName { get; set; }
    public string ClassName { get; set; }
    public MoonSharp.Interpreter.Table Fields;

    public float Health = 100;
    public float Armour = 0;

    public ENTITYTYPE EntityType = ENTITYTYPE.NONE;
    public MOVETYPE MoveType = MOVETYPE.NONE;

    public float MoveSpeed;
    private Vector3 _velocity;
    public Vector3 Velocity
    {
        get { return _velocity; }
        set 
        {            
            _velocity = value;
            if (Main.Network.IsNetworkMaster() && EntityType == ENTITYTYPE.PLAYER)
            {
                SetServerState(GlobalTransform.origin
                , value, ServerState.Rotation
                , Health, Armour);
            }
        }
    }

    public bool Emitting {
        get { return EntityNode.Particles != null ? EntityNode.Particles.Emitting : false; }
        set {
            if (EntityNode.Particles != null)
            {
                EntityNode.Particles.Emitting = value;
            }
        }
    }

    public uint CollisionLayer {
        get { 
            if (EntityType == ENTITYTYPE.PARTICLES)
            {
                return 0;
            }
            if (EntityNode == null || EntityNode.KinematicBody == null)
            {
                return 0;
            }
            return EntityNode.KinematicBody.CollisionLayer; 
        }
        set {
            if (EntityType == ENTITYTYPE.PARTICLES)
            {
                return;
            }
            if (EntityNode.KinematicBody.CollisionLayer != value)
            {
                EntityNode.KinematicBody.CollisionLayer = value;
            }
        }
    }

    public uint CollisionMask {
        get { 
            if (EntityType == ENTITYTYPE.PARTICLES)
            {
                return 0;
            }
            return EntityNode.KinematicBody.CollisionMask; 
        }
        set { 
            if (EntityType == ENTITYTYPE.PARTICLES)
            {
                return;
            }
            if (EntityNode.KinematicBody.CollisionMask != value)
            {
                EntityNode.KinematicBody.CollisionMask = value;
            } 
        }
    }

    public Transform GlobalTransform {
        get {
            if (this.EntityType == ENTITYTYPE.PARTICLES)
            {
                return EntityNode.Particles.GlobalTransform;
            }
            else
            {
                if (EntityNode.KinematicBody != null)
                {
                    return EntityNode.KinematicBody.GlobalTransform;
                }
                return new Transform();
            }
        }
        set {
            Transform transform = this.GlobalTransform;

            if (transform != value)
            {
                if (EntityNode.Entity.EntityType == ENTITYTYPE.PARTICLES)
                {
                    EntityNode.Particles.GlobalTransform = value;
                }
                else
                {
                    EntityNode.KinematicBody.GlobalTransform = value;
                }
            }
        }
    }
    
    public Vector3 OldOrigin;
    public Vector3 Origin { 
        get {
            return GlobalTransform.origin;
        }
        set {
            Transform t = GlobalTransform;

            if (value != OldOrigin)
                OldOrigin = value;

            t.origin = value;
            GlobalTransform = t;


            if (Main.Network.IsNetworkMaster() && EntityType == ENTITYTYPE.PLAYER)
            {
                SetServerState(value
                , ServerState.Velocity, ServerState.Rotation
                , Health, Armour);
            }
        }
    }
    public bool TouchingGround = false;
    public float Acceleration = 14.0f;
    public float Deceleration = 10.0f;
    
    [MoonSharpHidden]
    public DynValue TouchFunc;
    public string Touch {
        get { return TouchFunc.String; }
        set {
            TouchFunc = ScriptManager.ScriptServer.Globals.Get(value);
        }
    }

    public float NextThink = 0;
    [MoonSharpHidden]
    public DynValue ThinkFunc;
    public string Think {
        get { return ThinkFunc.String; }
        set {
            ThinkFunc = ScriptManager.ScriptServer.Globals.Get(value);
        }
    }

    // FIXME - testing, incorporate in to fields later
    public Dictionary<string, string> MapFields = new Dictionary<string, string>();

    public Entity()
    {
        if (Main.Network.IsNetworkMaster())
        {
            Fields = ScriptManager.GetFieldExtensions();

            EntityID = Main.World.EntityManager.GetEntityID();
        }
    }

    [MoonSharpHidden]
    public void InitPlayer(Client client)
    {
        NetName = client.NetworkID.ToString();
        EntityType = ENTITYTYPE.PLAYER;
        EntityID = (ushort)client.NetworkID;

        ClientOwner = client;
        ClassName = "player";
        MoveType = MOVETYPE.STEP;
        MoveSpeed = 32;
    }

    private bool CanAccelerate(Vector3 wishdir, float wishspeed)
    {
        float currentspeed = Velocity.Dot(wishdir);
        float addspeed = wishspeed - currentspeed;
        if(addspeed <= 0)
            return false;

        return true;
    }

    private Vector3 Accelerate(Vector3 wishdir, float wishspeed, float accel, float delta)
    {       
        float accelspeed = accel * delta * wishspeed;
        //if(accelspeed > addspeed)
         //   accelspeed = addspeed;
        Vector3 vel = Velocity;
        vel.x += accelspeed * wishdir.x;
        vel.z += accelspeed * wishdir.z;
        return vel;
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
        int max = (int)Mathf.Abs(pCmd.move_forward);
        if(Mathf.Abs(pCmd.move_right) > max)
            max = (int)Mathf.Abs(pCmd.move_right);
        if(max <= 0)
            return 0;

        float total = Mathf.Sqrt(pCmd.move_forward * pCmd.move_forward + pCmd.move_right * pCmd.move_right);
        float scale = MoveSpeed * max / (_moveScale * total);

        return scale;
    }

    public void SetServerState(Vector3 org, Vector3 velo, Vector3 rot, float health, float armour)
    {
        ServerState.Origin = org;
        ServerState.Velocity = velo;
        ServerState.Rotation = rot;
        Health = health;
        Armour = armour;
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
                    playerID = ClientOwner.NetworkID,
                    move_forward = 0,
                    move_right = 0,
                    move_up = 0,
                    basis = this.GlobalTransform.basis,
                    cam_angle = 0,
                    attack = 0
                    }
                );
        }
        else
        {
            pCmdQueue.Sort((x,y) => x.snapshot.CompareTo(y.snapshot));
        }

        Transform t = GlobalTransform;
        t.origin = PredictedState.Origin; // by this point it's a new serverstate
        GlobalTransform = t;

        foreach(PlayerCmd pCmd in pCmdQueue)
        {
            if (pCmd.snapshot <= ClientOwner.LastSnapshot)
            {
                continue;
            }
            if (ClientOwner != null && ClientOwner.NetworkID != Main.Network.GetTree().GetNetworkUniqueId())
            {
                Transform t2 = GlobalTransform;
                t2.basis = pCmd.basis;
                GlobalTransform = t2;
            }
            
            ClientOwner.LastSnapshot = pCmd.snapshot;
            this.Attack = pCmd.attack;
            DefaultProcess(pCmd, delta);
        }
    }

    public void PostFrame()
    {
        if (Main.Network.IsNetworkMaster())
        {
            SetServerState(GlobalTransform.origin, Velocity, EntityNode.KinematicBody.Rotation, Health, Armour);
        }
        else
        {
            Main.Network.SendPMovement(1, ClientOwner.NetworkID, pCmdQueue);
        }
        
        TrimCmdQueue();
        
        Main.ScriptManager.PlayerPostFrame(this);
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

        // FIXME - this is meant for antilag stuff...
        // if (Main.Network.IsNetworkMaster())
        // {
        //     int diff = Main.World.LocalSnapshot - pCmd.snapshot;
        //     if (diff < 0)
        //     {
        //         return;
        //     }
        //     //Main.World.RewindPlayers(diff, delta);
        // }

        //Main.World.FastForwardPlayers();
        
        if (MoveType == MOVETYPE.STEP)
        {
            this.ProcessMovementCmd(PredictedState, pCmd, delta);
        }
    }

    private void ProcessMovementCmd(State predState, PlayerCmd pCmd, float delta)
    {
        Velocity = predState.Velocity;

        // queue jump
        if (pCmd.move_up == 1 && !WishJump)
        {
            WishJump = true;
        }
        if (pCmd.move_up <= 0)
        {
            WishJump = false;
        }

        if (TouchingGround || OnLadder)
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

        Vector3 bz = pCmd.basis.z;
        bz.y = 0;
        Vector3 bx = pCmd.basis.x;
        bx.y = 0;

        wishDir += bx * pCmd.move_right;
        
        wishDir -= bz * pCmd.move_forward;
        wishDir = wishDir.Normalized();
        Vector3 moveDirectionNorm = wishDir;

        float wishSpeed = wishDir.Length();
        wishSpeed *= MoveSpeed;
        if (CanAccelerate(wishDir, wishSpeed))
        {
            Velocity = Accelerate(wishDir, wishSpeed, Deceleration, delta);
        }

        Vector3 vel = Velocity;
        if (OnLadder)
        {
            if (pCmd.move_forward != 0f)
            {
                vel.y = MoveSpeed * (pCmd.cam_angle / 90) * pCmd.move_forward;
            }
            else
            {
                vel.y = 0;
            }
            if (pCmd.move_right == 0f)
            {
                vel.x = 0;
                vel.z = 0;
            }
        }

        // walk up stairs
        if (wishSpeed > 0 && EntityNode.StairCatcher.IsColliding())
        {
            Vector3 col = EntityNode.StairCatcher.GetCollisionNormal();
            float ang = Mathf.Rad2Deg(Mathf.Acos(col.Dot(Main.World.Up)));
            if (ang > 0 && ang < _maxStairAngle)
            {
                vel.y = _stairJumpHeight;
            }
        }

        if (WishJump && EntityNode.KinematicBody.IsOnFloor())
        {
            vel.y += _jumpSpeed;
        }
        Velocity = vel;
    }

    private void AirMove(float delta, PlayerCmd pCmd)
    {
        Vector3 wishdir = new Vector3();
        
        float wishvel = _airAcceleration;
        float accel;
        
        float scale = CmdScale(pCmd);

        wishdir += pCmd.basis.x * pCmd.move_right;
        wishdir -= pCmd.basis.z * pCmd.move_forward;

        float wishspeed = wishdir.Length();
        wishspeed *= MoveSpeed;

        wishdir = wishdir.Normalized();
        Vector3 moveDirectionNorm = wishdir;

        // CPM: Aircontrol
        float wishspeed2 = wishspeed;
        if (Velocity.Dot(wishdir) < 0)
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

        if (CanAccelerate(wishdir, wishspeed))
        {
            Velocity = Accelerate(wishdir, wishspeed, Deceleration, delta);
        }
        /*if(_airControl > 0)
        {
            AirControl(wishdir, wishspeed2, delta);
        }*/
        // !CPM: Aircontrol       
    }

    public void InterpolateMesh(float delta)
    {
        if (EntityNode.MeshInstance == null)
        {
            //GD.Print("returning for: ", EntityNode.Entity.ClassName);
            return;
        }

        Main.World.MoveEntity(this.EntityNode, delta);
    }
}