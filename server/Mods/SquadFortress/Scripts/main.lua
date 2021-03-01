 -- defines a factorial function
 function fact (n)
    if (n == 0) then
        return 1
    else
        return Mul(n, fact(n - 1));
    end
end

function clientconnected (player)
    BPrint(player.NetName, " has joined the game");
end

function clientdisconnected (player)
    BPrint(player.NetName, " has left the game");
end