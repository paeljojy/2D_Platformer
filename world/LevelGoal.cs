using Godot;
using System;

public class LevelGoal : Area2D
{
    private bool Reached;

    public override void _Ready()
    {
        this.Reached = false;
    }

    public void OnBodyEnter(RigidBody2D body)
    {
        if (!Reached && body is Player)
        {
            this.Reached = true;
            AnimationPlayer animationPlayer = GetNode("Anim") as AnimationPlayer;
            animationPlayer.Play("Reached");
            GetTree().ChangeScene("Stage2");
        }
    }
}
