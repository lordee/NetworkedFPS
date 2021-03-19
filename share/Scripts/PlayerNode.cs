using Godot;

public class PlayerNode : Node
{
    // Nodes
    public Body Body;
    

    public Player Player;
    static private string _resource = Util.GetResourceString("PlayerNode.tscn", RESOURCE.SCENE);

    static public PlayerNode Instance()
    {
        PackedScene ps = ResourceLoader.Load(_resource) as PackedScene;
        PlayerNode player = ps.Instance() as PlayerNode;

        return player;
    }

    public void Init(Client client)
    {
        Name = client.NetworkID.ToString();
        Body = GetNodeOrNull("Body") as Body;
        Body.Init(this);

        Player = new Player(client, this);
    }
}