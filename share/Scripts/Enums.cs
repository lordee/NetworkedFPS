using System;
public enum MOVETYPE : UInt16
{
    NONE = 0,
    MISSILE = 1,
    STEP = 2,
    FLY = 3,
}

public enum PACKET : byte // entity type
{
    NONE,
    PLAYERID,
    PING,
    ROTATION,
    HEALTH,
    ARMOUR,
    ENTITYID,
    OWNERID,
    BASISX,
    BASISY,
    BASISZ,
    ORIGIN,
    VELOCITY,
    COLLISIONLAYER,
    COLLISIONMASK,
    MOVESPEED,
    MOVETYPE,
    PRINT,
    PRINT_HIGH,
    BSOUND,
    REMOVE,
    SPAWN,
    SNAPSHOT,
    PCMDSNAPSHOT,
    PCMDFORWARD,
    PCMDUP,
    PCMDRIGHT,
    PCMDCAMANGLE,
    PCMDATTACK,
    IMPULSE
}

public enum RESOURCE
{
    NONE,
    SCENE,
    SOUND,
    MAP
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