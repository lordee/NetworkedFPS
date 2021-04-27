function FieldExtensions ()
    local extensions = {
        team_no = 0,
        class_no = 0,
        allowteams = 0,
        attack_finished = 0,
        weapon = 0,
        damage = 0,
        take_damage = 0,
    };

    return extensions;
end

TEAM = {
    BLUE = 1,
    RED = 2,
}

STATE = {
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
    WEAPONTYPE = WEAPON.ROCKET
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

    ent.Fields.weapon = ROCKET.WEAPONTYPE;
    ent.Fields.damage = ROCKET.DAMAGE;
end

function RemoveEnt (entity)
    Remove(entity);
end

function Damage(targ, inflictor, damage, weaponType)
    if (targ.Fields.take_damage == FALSE) then
        return;
    end

    local damleft = damage;
    if (targ == inflictor.Owner) then
        damleft = damleft * .5;
    end

    local armleft = targ.CurrentArmour;
    local healthleft = targ.CurrentHealth;

    if (damleft >= armleft) then
        targ.CurrentArmour = 0;
        damleft = damleft - armleft;
    else
        targ.CurrentArmour = armleft - damleft;
        damleft = 0;
    end

    if (damleft >= healthleft) then
        targ.CurrentHealth = 0;
        Kill(targ);
        PrintDeathMessage(targ, inflictor, weaponType)
        damleft = damleft - healthleft;
    else
        targ.CurrentHealth = healthleft - damleft;
    end

    if (inflictor != nil and targ.MoveType == MOVETYPE.STEP) then
        local dir = targ.Origin - inflictor.Origin;
        dir = Normalise(dir);
        dir = dir * damage / SCALINGFACTOR;
        targ.Velocity = targ.Velocity + dir;
    end
end

function PrintDeathMessage(targ, inflictor, weaponType)
    local msg = "PrintDeathMessage not implemented";
    if (weaponType == WEAPON.ROCKET) then
        msg = targ.NetName .. " rides " .. inflictor.Owner.NetName .. "'s rocket";
    end

    BPrint(msg);
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
            Damage(ent, inflictor, dam, inflictor.Fields.weapon);
        end
    end
end

function RocketTouch (rocket, other)
    -- FIXME - support skies, remove if it's a sky

    local touched = "world";
    if (other != nil) then
        touched = other.NetName;

        Damage(other, rocket, rocket.Fields.damage, rocket.Fields.weapon);
    end

    RadiusDamage(rocket, other);

    BPrint(rocket.Owner.NetName, "'s rocket touched ", touched, " at ", rocket.Origin);

    local newent = Spawn("Weapons/Explosion.tscn");
    newent.GlobalTransform = rocket.GlobalTransform;
    -- FIXME - I think still causing issues? how do we do no collision, test by running over particles entity
    newent.CollisionLayer = 0;
    newent.CollisionMask = 0;
    newent.Emitting = true;

    newent.Think = "RemoveEnt";
    newent.NextThink = Time() + 1;

    RemoveEnt(rocket);
end

function Kill(targ)
    targ.State = STATE.DEAD;
    targ.MoveType = MOVETYPE.NONE;
    targ.ViewOffset = {0, 0, 0};
    -- TODO - network viewoffset
    -- TODO - body on ground   
end

-- FIXME - identify endless loops in lua somehow
-- FIXME - could result in unending loop if spawn does not exist?  Might be fixed
function PlayerSpawn (player)
    player.Fields.take_damage = TRUE;
    player.MoveType = MOVETYPE.STEP;
    player.State = STATE.ALIVE;
    player.ViewOffset = {0, 1.5, 0};

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
    player.State = STATE.ALIVE;
end