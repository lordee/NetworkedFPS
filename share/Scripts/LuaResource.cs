using System;
using Godot;

public class LuaResource
{
    private string _resource;
    public string Resource
    {
        get { return _resource; }
        set {
            _resource = value;
            PackedScene = ResourceLoader.Load(value) as PackedScene;
        }
    }
    public PackedScene PackedScene;

    public UInt16 ID;

    public LuaResource()
    {
    }
}