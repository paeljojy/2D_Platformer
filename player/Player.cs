using Godot;
using System;
using System.Text.RegularExpressions;

public class Player : RigidBody2D
{
    private static readonly float WALK_ACCELERATION = 800.0f;
    private static readonly float WALK_DEACCELERATION = 800.0f;
    private static readonly float WALK_MAX_VELOCITY = 200.0f;
    private static readonly float AIR_ACCELERATION = 200.0f;
    private static readonly float AIR_DEACCELERATION = 200.0f;
    private static readonly float JUMP_VELOCITY = 460.0f;
    private static readonly float STOP_JUMP_FORCE = 900.0f;
    private static readonly float MAX_SHOOT_POSE_TIME = 0.3f;
    private static readonly float MAX_FLOOR_AIRBORNE_TIME = 0.15f;

    private bool sidingLeft;
    private bool jumping;
    private bool stoppingJump;
    private bool shooting;
    private float floorHVelocity;
    private float airborneTime;
    private float shootTime;
    private String animation;
    private PackedScene bulletScene;
    private PackedScene enemyScene;
    private float InputHeldTime;
    private bool InputIsPressed;
    private bool bShouldMoveLeft;
    private float InputPressedAtTime;
    private ulong bLastChangedDirectionTime;

    void HandleInput()
    {
        bool input = Input.IsActionPressed("input");
        if (input)
        {
            // var CurrentInputTime = OS.GetTicksMsec();
            // GD.Print("Pressed time is: " + (CurrentInputTime - InputPressedAtTime));
            // if (CurrentInputTime - InputPressedAtTime <= 1000 * .5)
            // {
            // bShouldMoveLeft = !bShouldMoveLeft;
            // }
            // else
            // {
            // jumping = true;
            // }

            // GD.Print("Pressed time is: " + InputHeldTime);
            InputPressedAtTime = OS.GetTicksMsec();
        }
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        // bool input = Input.IsActionJustPressed("input");
        /*
        else
        {
            if (Input.IsActionJustReleased("input"))
            {
                if (InputHeldTime >= 0.2f)
                {
                    GD.Print("Pressed time was: " + InputHeldTime);
                    jumping = true;
                }
                else
                {
                    bShouldMoveLeft = !bShouldMoveLeft;
                }
            }

            InputHeldTime = 0;
        }
        */
    }

    public override void _Ready()
    {
        this.animation = "";
        this.sidingLeft = false;
        this.jumping = false;
        this.stoppingJump = false;
        this.shooting = false;
        this.floorHVelocity = 0.0f;
        this.airborneTime = Mathf.Pow(10, 20);
        this.shootTime = Mathf.Pow(10, 20);
        this.bulletScene = ResourceLoader.Load("res://player/Bullet.tscn") as PackedScene;
        this.enemyScene = ResourceLoader.Load("res://enemy/Enemy.tscn") as PackedScene;
    }

    public void ShotBullet()
    {
        this.shootTime = 0.0f;
        RigidBody2D bullet = bulletScene.Instance() as RigidBody2D;
        float side = sidingLeft ? -1.0f : 1.0f;
        Position2D bulletShoot = GetNode("BulletShoot") as Position2D;
        if (bulletShoot != null)
        {
            Vector2 bulletPosition = Position + bulletShoot.Position * (new Vector2(side, 1.0f));

            if (bullet != null) bullet.Position = bulletPosition;
        }

        GetParent().AddChild(bullet);

        bullet.LinearVelocity = new Vector2(800.0f * side, -80.0f);

        Particles2D particles = GetNode("Sprite/Smoke") as Particles2D;
        particles.Restart();
        AudioStreamPlayer2D soundShoot = GetNode("SoundShoot") as AudioStreamPlayer2D;
        soundShoot.Play();

        AddCollisionExceptionWith(bullet);
    }

    public override void _IntegrateForces(Physics2DDirectBodyState bodyState)
    {
        Vector2 linearVelocity = bodyState.LinearVelocity;
        float step = bodyState.Step;

        HandleInput();

        PlayerInputInteraction playerInputInteraction = ListenToPlayerInput();
        // ListenToPlayerInputAsync();

        linearVelocity.x -= this.floorHVelocity;
        floorHVelocity = 0.0f;

        FloorContact floorContact = FindFloorContact(bodyState);

        for (int i = 0; i < bodyState.GetContactCount(); ++i)
        {
            if (bodyState.GetContactColliderObject(i) is SwitchDirectionWall wall)
            {
                // GD.Print(wall.Name);
                // EmitSignal("Collided", coll);

                // Temporary regex check to check for walls that will switch our movement direction
                // Regex r = new Regex(@"SwitchDirectionWall.*", RegexOptions.None);
                // if (r.Match(colli.Name).Success)
                
                // Make sure we only switch once
                // (kinda also makes it so that even
                // if we have two walls really close to each other it doesn't feel "glitchy")
                var currentTime = OS.GetTicksMsec();
                if (currentTime - bLastChangedDirectionTime >= 1000 * 0.6)
                {
                    bShouldMoveLeft = !bShouldMoveLeft;
                    bLastChangedDirectionTime = currentTime;
                }
            }
        }

        ProcessSpawn(playerInputInteraction);
        ProcessShooting(playerInputInteraction, step);
        ProcessFloorContact(floorContact, step);
        linearVelocity = ProcessJump(playerInputInteraction, linearVelocity, step);
        linearVelocity = ProcessPlayerMovement(playerInputInteraction, linearVelocity, step);

        // this.shooting = playerInputInteraction.Shoot;
        if (floorContact.FoundFloor)
        {
            floorHVelocity = bodyState.GetContactColliderVelocityAtPosition(floorContact.FloorIndex).x;
            linearVelocity.x += floorHVelocity;
        }

        linearVelocity += bodyState.TotalGravity * step;
        bodyState.LinearVelocity = linearVelocity;
    }

    private PlayerInputInteraction ListenToPlayerInput()
    {
        // bool input = Input.IsActionJustReleased("input");
        // if (input)
        // {
        // InputIsPressed = true;
        // if (InputHeldTime > .5f)
        // {
        // bShouldMoveLeft = !bShouldMoveLeft;
        // }
        // }
        // else
        // {
        // InputIsPressed = false;
        // }

        bool spawn = Input.IsActionPressed("spawn");
        bool input = Input.IsActionPressed("input");

        // return new PlayerInputInteraction(false, spawn);
        return new PlayerInputInteraction(input, spawn);
    }

    private async void ListenToPlayerInputAsync()
    {
        bool input = Input.IsActionJustPressed("input");
        if (input)
        {
            InputIsPressed = true;
            Timer timer = new Timer();
            timer.WaitTime = 1;
            await ToSignal(timer, "timeout");
            if (Input.IsActionPressed("input"))
            {
                InputIsPressed = true;
                if (InputHeldTime > .5f)
                {
                    bShouldMoveLeft = !bShouldMoveLeft;
                }
            }
        }
        else
        {
            InputIsPressed = false;
        }
    }

    private void ProcessSpawn(PlayerInputInteraction playerInputInteraction)
    {
        if (playerInputInteraction.Spawn)
        {
            RigidBody2D enemy = this.enemyScene.Instance() as RigidBody2D;
            Vector2 position = Position;

            position.y = position.y - 100;
            if (enemy != null)
            {
                enemy.Position = position;

                GetParent().AddChild(enemy);
            }
        }
    }

    private void ProcessShooting(PlayerInputInteraction playerInputInteraction, float step)
    {
        if (/*playerInputInteraction.Shoot &&*/ !this.shooting && shootTime > 1.0f)
        {
            CallDeferred("ShotBullet");
        }
        else
        {
           this.shootTime += step;
        }
    }

    private void ProcessFloorContact(FloorContact floorContact, float step)
    {
        if (floorContact.FoundFloor)
        {
            this.airborneTime = 0.0f;
        }
        else
        {
            this.airborneTime += step;
        }
    }

    private Vector2 ProcessJump(PlayerInputInteraction playerInputInteraction, Vector2 linearVelocity, float step)
    {
        if (!this.jumping)
        {
            return linearVelocity;
        }

        if (linearVelocity.y > 0)
        {
            this.jumping = false;
        }
        else if (!playerInputInteraction.Input)
        {
            this.stoppingJump = true;
        }

        if (this.stoppingJump)
        {
            linearVelocity.y += STOP_JUMP_FORCE * step;
        }

        return linearVelocity;
    }

    private Vector2 ProcessPlayerMovement(PlayerInputInteraction playerInputInteraction, Vector2 linearVelocity,
        float step)
    {
        bool onFloor = airborneTime < MAX_FLOOR_AIRBORNE_TIME;

        if (onFloor)
        {
            linearVelocity = ProcessPlayerDirectionalMovement(playerInputInteraction, linearVelocity, step);
            linearVelocity = ProcessJumpMovement(playerInputInteraction, linearVelocity, step);
            ProcessPlayerSiding(playerInputInteraction, linearVelocity);
            ProcessAnimation(playerInputInteraction, linearVelocity);
        }
        else
        {
            linearVelocity = ProcessPlayerInAirDirectionalMovement(playerInputInteraction, linearVelocity, step);
            ProcessInAirAnimation(playerInputInteraction, linearVelocity);
        }

        return linearVelocity;
    }

    private FloorContact FindFloorContact(Physics2DDirectBodyState bodyState)
    {
        FloorContact floorContact = new FloorContact(false, -1);

        for (int i = 0; i < bodyState.GetContactCount(); i++)
        {
            Vector2 contactLocalNormal = bodyState.GetContactLocalNormal(i);

            if (contactLocalNormal.Dot(new Vector2(0, -1)) > 0.6f)
            {
                floorContact.FoundFloor = true;
                floorContact.FloorIndex = i;
            }
        }

        return floorContact;
    }

    private Vector2 ProcessPlayerDirectionalMovement(PlayerInputInteraction playerInputInteraction,
        Vector2 linearVelocity, float step)
    {
        if (bShouldMoveLeft /* && !playerInputInteraction.MoveRight */)
        {
            if (linearVelocity.x > -WALK_MAX_VELOCITY)
            {
                linearVelocity.x -= WALK_ACCELERATION * step;
            }
            // InputIsPressed = true;
        }
        // else if (playerInputInteraction.MoveRight && !playerInputInteraction.MoveLeft)
        else
        {
            // InputIsPressed = false;
            if (linearVelocity.x < WALK_MAX_VELOCITY)
            {
                linearVelocity.x += WALK_ACCELERATION * step;
            }
        }
        // else
        // {
        // float linearVelocityX = Mathf.Abs(linearVelocity.x);
        // linearVelocityX -= WALK_DEACCELERATION * step;
        // linearVelocityX = linearVelocityX < 0 ? 0 : linearVelocityX;
        // linearVelocity.x = Mathf.Sign(linearVelocity.x) * linearVelocityX;
        // }

        return linearVelocity;
    }

    private Vector2 ProcessPlayerInAirDirectionalMovement(PlayerInputInteraction playerInputInteraction,
        Vector2 linearVelocity, float step)
    {
        // if (playerInputInteraction.MoveLeft && !playerInputInteraction.MoveRight)
        if (bShouldMoveLeft)
        {
            if (linearVelocity.x > -WALK_MAX_VELOCITY)
            {
                linearVelocity.x -= AIR_ACCELERATION * step;
            }
        }
        // if (playerInputInteraction.MoveRight && !playerInputInteraction.MoveLeft)
        // {
        else
        {
            if (linearVelocity.x < WALK_MAX_VELOCITY)
            {
                linearVelocity.x += AIR_ACCELERATION * step;
            }
        }
        // }
        // else
        // {
        //     float linearVelocityX = Mathf.Abs(linearVelocity.x);
        //     linearVelocityX -= AIR_DEACCELERATION * step;
        //     linearVelocityX = linearVelocityX < 0 ? 0 : linearVelocityX;
        //     linearVelocity.x = Mathf.Sign(linearVelocity.x) * linearVelocityX;
        // }

        return linearVelocity;
    }

    private Vector2 ProcessJumpMovement(PlayerInputInteraction playerInputInteraction, Vector2 linearVelocity,
        float step)
    {
        // if (this.jumping /* && playerInputInteraction.Jump*/)
        if (!this.jumping && playerInputInteraction.Input)
        {
            linearVelocity.y = -JUMP_VELOCITY;
            this.jumping = true;
            this.stoppingJump = false;
            AudioStreamPlayer2D soundJump = GetNode("SoundJump") as AudioStreamPlayer2D;
            soundJump.Play();
        }

        return linearVelocity;
    }

    private void ProcessPlayerSiding(PlayerInputInteraction playerInputInteraction, Vector2 linearVelocity)
    {
        bool newSidingLeft = this.sidingLeft;
        if (linearVelocity.x < 0)
            // if (linearVelocity.x < 0 && playerInputInteraction.MoveLeft)
        {
            newSidingLeft = true;
        }
        // else if (linearVelocity.x > 0 && playerInputInteraction.MoveRight)
        else if (linearVelocity.x > 0)
        {
            newSidingLeft = false;
        }

        UpdateSidingLeft(newSidingLeft);
    }

    private void ProcessAnimation(PlayerInputInteraction playerInputInteraction, Vector2 linearVelocity)
    {
        String newAnimation = this.animation;
        if (this.jumping)
        {
            newAnimation = "jumping";
        }
        else if (Mathf.Abs(linearVelocity.x) < 0.1)
        {
            if (this.shootTime < MAX_SHOOT_POSE_TIME)
            {
                newAnimation = "idle_weapon";
            }
            else
            {
                newAnimation = "idle";
            }
        }
        else
        {
            if (this.shootTime < MAX_SHOOT_POSE_TIME)
            {
                newAnimation = "run_weapon";
            }
            else
            {
                newAnimation = "run";
            }
        }

        UpdateAnimation(newAnimation);
    }

    private void ProcessInAirAnimation(PlayerInputInteraction playerInputInteraction, Vector2 linearVelocity)
    {
        String newAnimation = this.animation;
        if (linearVelocity.y < 0)
        {
            if (this.shootTime < MAX_SHOOT_POSE_TIME)
            {
                newAnimation = "jumping_weapon";
            }
            else
            {
                newAnimation = "jumping";
            }
        }
        else
        {
            if (this.shootTime < MAX_SHOOT_POSE_TIME)
            {
                newAnimation = "falling_weapon";
            }
            else
            {
                newAnimation = "falling_weapon";
            }
        }

        UpdateAnimation(newAnimation);
    }

    private void UpdateSidingLeft(bool newSidingLeft)
    {
        Sprite sprite = GetNode("Sprite") as Sprite;
        Vector2 scale = sprite.Scale;
        if (!newSidingLeft.Equals(this.sidingLeft))
        {
            if (newSidingLeft)
            {
                scale.x = -1;
            }
            else
            {
                scale.x = 1;
            }
        }

        sprite.Scale = scale;
        this.sidingLeft = newSidingLeft;
    }

    private void UpdateAnimation(String newAnimation)
    {
        if (!newAnimation.Equals(this.animation))
        {
            this.animation = newAnimation;
            AnimationPlayer animationPlayer = GetNode("Anim") as AnimationPlayer;
            animationPlayer.Play(this.animation);
        }
    }
}
