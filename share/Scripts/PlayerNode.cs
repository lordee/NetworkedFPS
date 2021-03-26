using Godot;

public class PlayerNode : EntityNode
{
    // Nodes
    public Spatial Head;
    public RayCast StairCatcher;
    
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
        Head = (Spatial)GetNode("Head");
        StairCatcher = (RayCast)GetNode("StairCatcher");
        MeshInstance = GetNode("MeshInstance") as MeshInstance;

        Player = new Player(client, this);
        Entity = Player;
    }

    public void RotateHead(float rad)
    {
        Head.RotateY(rad);
        MeshInstance.RotateY(rad);
    }
}