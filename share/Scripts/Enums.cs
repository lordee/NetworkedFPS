public enum MOVETYPE
{
    NONE = 0,
    MISSILE = 1,
    STEP = 2,
    FLY = 3,
}

public enum PACKETTYPE // entity type
{
    NONE,
    PLAYER,
    PROJECTILE,
    PRINT,
    PRINT_HIGH,
    BSOUND,
}

public enum RESOURCE
{
    NONE,
    SCENE,
    SOUND,
    MAP
}

public class PACKET
{
    public const string IMPULSE = @"\p";
    public const string HEADER = @"\h";
    public const string END = @"\e";
}

public enum PACKETSTATE
{
    UNINITIALISED,
    HEADER,
    IMPULSE,
    END,
}

public enum PSTATE
{
    DEAD,
    ALIVE,
}

public class ButtonInfo
{
	public enum TYPE {UNSET, SCANCODE, MOUSEBUTTON, MOUSEWHEEL, MOUSEAXIS, CONTROLLERBUTTON, CONTROLLERAXIS}
	public enum DIRECTION {UP, DOWN, RIGHT, LEFT};
}

public enum COMMANDTYPE
{
    PLAYERCONTROLLER,
    COMMAND
}