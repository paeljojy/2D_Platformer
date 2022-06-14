using Godot;
using System;

public class LevelGoal : Area2D
{
    private bool Reached;

    [Export] private PackedScene nextLevel;
    
    public override void _Ready()
    {
        this.Reached = false;
    }

    public void OnGoalReached(RigidBody2D body)
    {
        if (!Reached && body is Player)
        {
            this.Reached = true;
            AnimationPlayer animationPlayer = GetNode("Anim") as AnimationPlayer;
            animationPlayer.Play("Reached");
            GD.Print("GOAL REACHED!");
            GetTree().ChangeScene(nextLevel.ResourcePath);
        }
    }
}
