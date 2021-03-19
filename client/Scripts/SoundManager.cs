using Godot;
using System;

public class SoundManager : Node
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

    public void Sound3D(Vector3 origin, string resource)
    {
        AudioStreamPlayer3D node = new AudioStreamPlayer3D();
        AddChild(node);

        // TODO - support ogg/mp3 etc
        AudioStreamSample sound = ResourceLoader.Load(resource) as AudioStreamSample;

        node.Stream = sound;
        Transform t = node.GlobalTransform;
        t.origin = origin;
        node.GlobalTransform = t;

        node.Connect("finished", this, nameof(SoundFinished), new Godot.Collections.Array() { node });
        node.Play();
    }

    public void SoundFinished(Node node)
    {
        this.RemoveChild(node);
    }
}
