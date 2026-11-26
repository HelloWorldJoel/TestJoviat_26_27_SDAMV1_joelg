using UnityEngine;
[RequireComponent(typeof(Collider2D))]
public class GravityController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum Direction { Left, Right, Up, Down }

    public Direction unlockedDirection = Direction.Left;
    public bool resetGravityOnExit = true;   // go back to normal gravity when leaving

    Vector2 ToVector()
    {
        switch (unlockedDirection)
        {
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            case Direction.Up: return Vector2.up;
            default: return Vector2.down;
        }
    }

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<Gravity>();
        if (player != null) player.AllowDirection(ToVector());
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<Gravity>();
        if (player != null) player.DisallowDirection(ToVector(), resetGravityOnExit);
    }
}
