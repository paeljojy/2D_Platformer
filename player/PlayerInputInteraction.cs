public class PlayerInputInteraction
{

    public bool Input { get; set; }
    public bool Spawn { get; set; }

    public PlayerInputInteraction(bool input, bool spawn)
    {
        this.Input = input;
        this.Spawn = spawn;
    }

}
