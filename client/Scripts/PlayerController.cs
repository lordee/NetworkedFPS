using Godot;
using System;
using System.Collections.Generic;


public class PlayerController : Camera
{
    static string _playerControllerResource = "res://Scenes/PlayerController.tscn";
    PlayerNode _playerNode;
    public PlayerNode PlayerNode { get { return _playerNode; }}

    // Player commands, stores wish commands that the player asks for (Forward, back, jump, etc)
    private float move_forward = 0;
    private float move_right = 0;
    private float move_up = 0;
    private int attack = 0;
    private float _cameraAngle = 0f;
    private Vector3 shootTo = new Vector3();
    private float _shootRange = 100000f;
    List<float> impulses = new List<float>();

    public override void _Ready()
    {
    }

    public void Init(PlayerNode p)
    {
        _playerNode = p;
    }

    static public PlayerController Instance()
    {
        PackedScene controller = ResourceLoader.Load(_playerControllerResource) as PackedScene;
        PlayerController pc = controller.Instance() as PlayerController;

        return pc;
    }

    public void Attach(PlayerNode playerNode)
    {
        Node parent = this.GetParent();
        if (parent != null)
        {
            parent.RemoveChild(this);
        }
        playerNode.Head.AddChild(this);
        playerNode.MeshInstance.Visible = false; // TODO - just remove it
        Main.PlayerController = this;
        Init(playerNode);
        SetProcess(true);
        Notification(NotificationReady);
    }

    public override void _PhysicsProcess(float delta)
    {
        shootTo = new Vector3();
        // FIXME - spawn projectile from middle of player, not camera?
        Vector3 origin = ProjectRayOrigin(UIManager.HUD.AimAt.Position);
        Vector3 to = ProjectRayNormal(UIManager.HUD.AimAt.Position) * _shootRange;
        shootTo = to + origin;
        
        PlayerCmd pCmd = new PlayerCmd();
        pCmd.playerID = _playerNode.Player.NetworkID;
        pCmd.snapshot = Main.World.LocalSnapshot;
        pCmd.move_forward = move_forward;
        pCmd.move_right = move_right;
        pCmd.move_up = move_up;
        pCmd.basis = this.GlobalTransform.basis;
        pCmd.cam_angle = _cameraAngle;
        // FIXME - basis.z.angleto(up vector) instead of rotation value
        pCmd.attack = attack;
        pCmd.impulses = impulses;
        impulses.Clear();
        _playerNode.Player.pCmdQueue.Add(pCmd);
    }

    [InputWithArg(typeof(PlayerController), nameof(MoveForward))]
    public static void MoveForward(float val)
    {
        if (Main.PlayerController != null)
        {
            Main.PlayerController.move_forward += val;
        }
    }

    [InputWithArg(typeof(PlayerController), nameof(MoveBack))]
    public static void MoveBack(float val)
    {
        if (Main.PlayerController != null)
        {
            Main.PlayerController.move_forward -= val;
        }
    }

    [InputWithArg(typeof(PlayerController), nameof(MoveRight))]
    public static void MoveRight(float val)
    {
        if (Main.PlayerController != null)
        {
            Main.PlayerController.move_right += val;
        }
    }

    [InputWithArg(typeof(PlayerController), nameof(MoveLeft))]
    public static void MoveLeft(float val)
    {
        if (Main.PlayerController != null)
        {
            Main.PlayerController.move_right -= val;
        }
    }

    [InputWithArg(typeof(PlayerController), nameof(Jump))]
    public static void Jump(float val)
    {
        if (Main.PlayerController != null)
        {
            Main.PlayerController.move_up = val;
        }
    }

    [InputWithArg(typeof(PlayerController), nameof(Attack))]
    public static void Attack(float val)
    {
        // FIXME - setinputashandle is not working on closing of UI, breaks the game when in lobby and click with mouse to close something, it sets off an attack command while client is null
        if (Main.PlayerController != null)
        {
            Main.PlayerController.attack = Convert.ToInt32(val);
        }
    }

    [InputWithoutArg(typeof(PlayerController), nameof(MouseModeToggle))]
    public static void MouseModeToggle()
    {
        Settings.MouseCursorVisible = !Settings.MouseCursorVisible;
        if (Settings.MouseCursorVisible)
        {
            Input.SetMouseMode(Input.MouseMode.Visible);
        }
        else
        {
            Input.SetMouseMode(Input.MouseMode.Captured);
        }
    }

    [InputWithArg(typeof(PlayerController), nameof(LookUp))]
	public static void LookUp(float val)
	{
        if (Main.PlayerController != null)
        {
            if (val > 0)
            {
                float change = val * Settings.Sensitivity * Settings.InvertMouseValue;
                if (Main.PlayerController._cameraAngle + change < 90f && Main.PlayerController._cameraAngle + change > -90f)
                {
                    Main.PlayerController._cameraAngle += change;
                    Main.PlayerController.RotateX(Mathf.Deg2Rad(change));
                }
            }
        }
	}

	[InputWithArg(typeof(PlayerController), nameof(LookDown))]
	public static void LookDown(float val)
	{
        if (Main.PlayerController != null)
        {
            if (val > 0)
            {
                float change = -val * Settings.Sensitivity * Settings.InvertMouseValue;
                if (Main.PlayerController._cameraAngle + change < 90f && Main.PlayerController._cameraAngle + change > -90f)
                {
                    Main.PlayerController._cameraAngle += change;
                    Main.PlayerController.RotateX(Mathf.Deg2Rad(change));
                }
            }
        }
	}

	[InputWithArg(typeof(PlayerController), nameof(LookRight))]
	public static void LookRight(float val)
	{
        if (Main.PlayerController != null)
        {
            if (val > 0)
            {
                float change = Mathf.Deg2Rad(-val * Settings.Sensitivity);
                Main.PlayerController._playerNode.RotateHead(change);
            }
        }
	}

	[InputWithArg(typeof(PlayerController), nameof(LookLeft))]
	public static void LookLeft(float val)
	{
        if (Main.PlayerController != null)
        {
            if (val > 0)
            {
                float change = Mathf.Deg2Rad(val * Settings.Sensitivity);
                Main.PlayerController._playerNode.RotateHead(change);
            }
        }
	}
}
