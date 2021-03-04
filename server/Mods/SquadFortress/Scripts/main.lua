function fieldExtensions ()
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

lastspawn_team1 = nil;
lastspawn_team2 = nil;

function clientConnected (player)
    BPrint(player.NetName, " has joined the game");
end

function clientDisconnected (player)
    BPrint(player.NetName, " has left the game");
end

function playerSpawn (player)     
    local team = player.Fields["team_no"];

    local spawn = nil;
    if (team == TEAM.BLUE) then
        spawn = lastspawn_team1;
    else
        spawn = lastspawn_team2;
    end

    -- find next spawn after last spawn 
    local spawn = Find(spawn, "classname", "info_player_team");
    local spawnFound = false;
    while (spawn != nil)
    do
        if (team == spawn.Fields["team_no"]) then
            spawnFound = true;
            break;
        end

        spawn = Find(spawn, "classname", "info_player_team");
    end

    if spawn
end