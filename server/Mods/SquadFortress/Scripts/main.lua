function FieldExtensions ()
    local extensions = {
        team_no = 0,
        class_no = 0,
        allowteams = 0,
        attack_finished = 0,
        weapon = 0,
        damage = 0,
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

ROCKET = {
    DAMAGE = 100,
    CLASSNAME = "proj_rocket",
    TOUCH = "RocketTouch",
    MOVESPEED = 90,
    MOVETYPE = MOVETYPE.MISSILE,
    THINK = "RemoveEnt",
    NEXTTHINK = 5,
    ATTACK_FINISHED = 0.8,
}

FALSE = 0;
TRUE = 1;
SCALINGFACTOR = 2; -- random scaling factor, thought it was 10:1...


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
    if (player.Fields.attack_finished <= Time()) then
        FireRocket(player);
    end
end

function FireRocket (shooter)
    local t = Time();
    shooter.Fields.attack_finished = t + ROCKET.ATTACK_FINISHED;
    -- create entity
    BSound(shooter.Origin, "shots/rocket.wav");
    
    -- TODO - use scenes for now? but then do we need bsound etc?
    local ent = Spawn("Weapons/Rocket.tscn");
    ent.Owner = shooter;
    ent.MoveType = ROCKET.MOVETYPE;
    ent.GlobalTransform = shooter.GlobalTransform;
    ent.MoveSpeed = ROCKET.MOVESPEED;
    ent.Touch = ROCKET.TOUCH;
    ent.NextThink = Time() + ROCKET.NEXTTHINK;
    ent.Think = ROCKET.THINK;
    ent.ClassName = ROCKET.CLASSNAME;

    ent.Fields.weapon = WEAPON.ROCKET;
    ent.Fields.damage = ROCKET.DAMAGE;
end

function RemoveEnt (entity)
    Remove(entity);
end

function Damage(other, inflictor, damage)
    local damleft = damage;
    if (other == inflictor.Owner) then
        damleft = damleft * .5;
    end

    local armleft = other.CurrentArmour;
    local healthleft = other.CurrentHealth;

    if (damleft >= armleft) then
        other.CurrentArmour = 0;
        damleft = damleft - armleft;
    else
        other.CurrentArmour = armleft - damleft;
        damleft = 0;
    end

    if (damleft >= healthleft) then
        other.CurrentHealth = 0;
        --Kill(other);
        damleft = damleft - healthleft;
    else
        other.CurrentHealth = healthleft - damleft;
    end

    if (inflictor != nil and other.MoveType == MOVETYPE.STEP) then
        local dir = other.Origin - inflictor.Origin;
        dir = Normalise(dir);
        dir = dir * damage / SCALINGFACTOR;
        other.Velocity = other.Velocity + dir;
    end
end

function RadiusDamage(inflictor, other)
    -- loop through ents in radius
    -- if they aren't other then do damage as percentage
    local rad = (inflictor.Fields.damage + 40) / 10;
    local ents = FindRadius(inflictor.Origin, rad);
    local count = ents.Count - 1;
    
    for i = 0, count do
        local ent = ents[i];
        if (ent != inflictor and ent != other) then
            local dist = VLen(inflictor.Origin, ent.Origin);
            local dam = inflictor.Fields.damage * ((inflictor.Fields.damage + 40 - dist) / (inflictor.Fields.damage + 40));
            Damage(ent, inflictor, dam);
        end
    end
end

function RocketTouch (entity, other)
    -- FIXME - support skies, remove if it's a sky

    local touched = "world";
    if (other != nil) then
        touched = other.NetName;

        Damage(other, entity, entity.Fields.damage);
    end

    RadiusDamage(entity, other);

    BPrint(entity.Owner.NetName, "'s rocket touched ", touched, " at ", entity.Origin);

    local newent = Spawn("Weapons/Explosion.tscn");
    --newent.GlobalTransform = entity.GlobalTransform;
    -- FIXME - I think still causing issues? how do we do no collision, test by running over particles entity
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