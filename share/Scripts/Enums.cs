public enum MOVETYPE
{
    NONE,
    MISSILE,
    STEP,
    FLY
}

public enum ENTITYTYPE // entity type
{
    PLAYER = 1,
    PROJECTILE = 2,
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