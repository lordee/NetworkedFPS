function FieldExtensions ()
    local extensions = {
        team_no = 0,
        class_no = 0,
        allowteams = 0,
        attack_finished = 0
    };

    return extensions;
end

TEAM = {
    BLUE = 1,
    RED = 2,
}

PSTATE = {
    DEAD = 0,
    ALIVE = 1,
}

MOVETYPE = {
    NONE = 0,
    MISSILE = 1,
    STEP = 2,
    FLY = 3,
}

WEAPON = {
    NONE = 0,
    ROCKET = 1,
}

FALSE = 0;
TRUE = 1;


lastspawn_team1 = nil;
lastspawn_team2 = nil;

-- map entities, all those that have a spawn function named
function ProcessEntity(entity)
    BPrint("Processing entity ", entity.NetName);
    entity.Fields.team_no = tonumber(entity.MapFields.team_no);
end

function info_player_teamspawn (entity)
    
end

function WorldPostLoad ()
    Precache_Sound("shots/rocket.wav");
    Precache_Scene("Weapons/Rocket.tscn");
    Precache_Scene("Weapons/Explosion.tscn");
end

function ClientConnected (player)
    BPrint(player.NetName, " has joined the game");
    player.Fields.team_no = 1;
end

function ClientDisconnected (player)
    BPrint(player.NetName, " has left the game");
end

function PlayerPostFrame (player)
    if (player.Attack == TRUE) then
        PlayerAttack(player);
    end
end

function PlayerAttack (player)
    local t = Time();
    if (player.Fields.attack_finished <= t) then
        player.Fields.attack_finished = t + 0.8;
        FireRocket(player);
    end
end

function FireRocket (shooter)
    -- create entity
    BSound(shooter.Origin, "shots/rocket.wav");
    
    -- TODO - use scenes for now? but then do we need bsound etc?
    local ent = Spawn("Weapons/Rocket.tscn");
    ent.Owner = shooter;
    ent.MoveType = MOVETYPE.MISSILE;
    ent.GlobalTransform = shooter.GlobalTransform;
    ent.MoveSpeed = 90;
    ent.Touch = "RocketTouch";
    ent.NextThink = Time() + 5;
    ent.Think = "RemoveEnt";
    ent.ClassName = "proj_rocket";

    ent.Fields.Weapon = WEAPON.ROCKET;
end

function RemoveEnt (entity)
    Remove(entity);
end

function RocketTouch (entity, other)
    local touched = "world";
    if (other != nil) then
        touched = other.NetName;
    end
    BPrint(entity.Owner.NetName, "'s rocket touched ", touched, " at ", entity.Origin);

    local newent = Spawn("Weapons/Explosion.tscn");
    newent.GlobalTransform = entity.GlobalTransform;
    newent.CollisionLayer = 0;
    newent.CollisionMask = 0;
    newent.Emitting = true;

    newent.Think = "RemoveEnt";
    newent.NextThink = Time() + 1;

    RemoveEnt(entity);
end

-- FIXME - identify endless loops in lua somehow
-- FIXME - could result in unending loop if spawn does not exist?  Might be fixed
function PlayerSpawn (player)     
    local team = player.Fields.team_no;
    local spawn = nil;
    
    if (team == TEAM.BLUE) then
        spawn = lastspawn_team1;
    else
        spawn = lastspawn_team2;
    end

    local start = true;
    if (spawn != nil) then
        -- we know that loop is not starting from nil
        start = false;
    end
    -- find next spawn after last spawn 
    local spawn = Find(spawn, "classname", "info_player_teamspawn");
    local spawnFound = false;
    while (spawn != nil)
    do
        if (team == spawn.Fields.team_no) then
            spawnFound = true;
            break;
        end

        spawn = Find(spawn, "classname", "info_player_teamspawn");
        if (spawn == nil) then
            if (start == false) then
                -- try again in case we started middle of loop
                spawn = Find(spawn, "classname", "info_player_teamspawn");
                start = true; -- now at start of loop
            else
                break; -- nil and we did our double search
            end
        end
    end

    if spawnFound == false then
        BPrint("spawn not found");
        return;
    end

    -- spawn found
    player.Origin = spawn.Origin;
    player.PState = PSTATE.ALIVE;
end