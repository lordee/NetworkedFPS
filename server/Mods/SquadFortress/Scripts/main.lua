function FieldExtensions ()
    local extensions = {
        "team_no",
        "class_no",
        "allowteams"
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

lastspawn_team1 = nil;
lastspawn_team2 = nil;

function ProcessEntity(entity)
    BPrint("Processing entity ", entity.NetName);
    entity.Fields.team_no = tonumber(entity.MapFields.team_no);
end

function info_player_teamspawn (entity)
    

end

function ClientConnected (player)
    BPrint(player.NetName, " has joined the game");
    player.Fields.team_no = 1;
end

function ClientDisconnected (player)
    BPrint(player.NetName, " has left the game");
end

-- FIXME - could result in unending loop if spawn does not exist
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