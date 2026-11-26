using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class Gravity: MonoBehaviour
{
    public float jumpForce = 8f;
    bool isGrounded;
    bool jumpPressed;
    public float gravityStrength = 9.81f;
    public float moveSpeed = 5f;
    public float dashSpeed = 25f;      // initial burst
    public float dashDecay = 60f;      // how fast it fades (units/second). 15 / 60 = 0.25s
    public float dashCooldown = 0.5f;
    [Header("Dash afterimages")]
    public SpriteRenderer spriteRenderer;      // drag your player's sprite here (or leave empty to auto-find)
    public float ghostInterval = 0.03f;        // time between ghosts
    public float ghostLifetime = 0.3f;         // how long each ghost takes to fade
    public Color ghostColor = new Color(0.5f, 0.8f, 1f, 0.6f);
    float nextGhostTime;

    Vector2 dashVelocity;
    float lastMoveSign = 1f;           // remembers the last direction you moved
    float nextDashTime;
    bool dashPressed;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.1f;
    public float groundCheckDistance = 0.05f;

    float lastGroundedTime = -1f;
    float lastJumpPressedTime = -1f;

    Collider2D col;
    ContactFilter2D groundFilter;
    readonly RaycastHit2D[] hits = new RaycastHit2D[4];

    Rigidbody2D rb;
    Vector2 gravityDir = Vector2.down;
    Vector2 moveInput;
    public float rotateSpeed = 720f;   // degrees per second
    float targetAngle;

    // Directions currently unlocked (Down is always allowed)
    readonly HashSet<Vector2> allowed = new HashSet<Vector2> { Vector2.down };

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        
        groundFilter = new ContactFilter2D { useTriggers = false }; // ignore your gravity zones
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        SetGravity(Vector2.down);
        
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        


        

        if (kb.leftArrowKey.wasPressedThisFrame) TrySetGravity(Vector2.left);
        if (kb.rightArrowKey.wasPressedThisFrame) TrySetGravity(Vector2.right);
        if (kb.upArrowKey.wasPressedThisFrame) TrySetGravity(Vector2.up);
        if (kb.downArrowKey.wasPressedThisFrame) TrySetGravity(Vector2.down);
        if (kb.spaceKey.wasPressedThisFrame) lastJumpPressedTime = Time.time;


        float h = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float v = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
        moveInput = new Vector2(h, v);
        if (kb.fKey.wasPressedThisFrame) dashPressed = true;//gets rthe input

        // Direction along the walking axis (x when gravity is vertical, y when horizontal)
        float walkInput = (gravityDir.y != 0) ? moveInput.x : moveInput.y;// states 
        if (walkInput != 0) lastMoveSign = Mathf.Sign(walkInput);
        // Afterimages while the dash is still active
        if (dashVelocity.sqrMagnitude > 1f && Time.time >= nextGhostTime)
        {
            SpawnGhost();
            nextGhostTime = Time.time + ghostInterval;
        }
    }

    void FixedUpdate()
    {
        rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotateSpeed * Time.fixedDeltaTime);
        Vector2 vel = rb.linearVelocity;
        Vector2 walkAxis = (gravityDir.y != 0) ? Vector2.right : Vector2.up;

        if (CheckGrounded()) lastGroundedTime = Time.time;

        // Dash
        if (dashPressed && Time.time >= nextDashTime)
        {
            dashVelocity = walkAxis * lastMoveSign * dashSpeed;
            nextDashTime = Time.time + dashCooldown;
        }
        dashPressed = false;
        dashVelocity = Vector2.MoveTowards(dashVelocity, Vector2.zero, dashDecay * Time.fixedDeltaTime);

        // Walk
        if (gravityDir.y != 0) vel.x = moveInput.x * moveSpeed + dashVelocity.x;
        else vel.y = moveInput.y * moveSpeed + dashVelocity.y;

        // Jump
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBuffer;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (wantsJump && canJump)
        {
            if (gravityDir.y != 0) vel.y = -gravityDir.y * jumpForce;
            else vel.x = -gravityDir.x * jumpForce;

            lastJumpPressedTime = -1f;  // consume the press
            lastGroundedTime = -1f;     // no double jump from coyote time
        }

        rb.linearVelocity = vel;
    }
    bool CheckGrounded()
    {
        int count = col.Cast(gravityDir, groundFilter, hits, groundCheckDistance);
        for (int i = 0; i < count; i++)
        {
            if (Vector2.Dot(hits[i].normal, -gravityDir) > 0.7f) return true;
        }
        return false;
    }
    void TrySetGravity(Vector2 dir)
    {
        if (allowed.Contains(dir)) SetGravity(dir);
    }

    void SetGravity(Vector2 dir)
    {
        gravityDir = dir;
        Physics2D.gravity = dir * gravityStrength;
        targetAngle = Vector2.SignedAngle(Vector2.down, dir);
    }

    // Called by the zones
    public void AllowDirection(Vector2 dir) { allowed.Add(dir); }

    public void DisallowDirection(Vector2 dir, bool resetIfActive)
    {
        if (dir == Vector2.down) return;           // Down is always allowed
        allowed.Remove(dir);
        if (resetIfActive && gravityDir == dir) SetGravity(Vector2.down);
    }
    void SpawnGhost()
    {
        if (spriteRenderer == null) return;

        var ghost = new GameObject("AfterImage");
        ghost.transform.position = spriteRenderer.transform.position;
        ghost.transform.rotation = spriteRenderer.transform.rotation;
        ghost.transform.localScale = spriteRenderer.transform.lossyScale;

        var gsr = ghost.AddComponent<SpriteRenderer>();
        gsr.sprite = spriteRenderer.sprite;
        gsr.flipX = spriteRenderer.flipX;
        gsr.flipY = spriteRenderer.flipY;
        gsr.sortingLayerID = spriteRenderer.sortingLayerID;
        gsr.sortingOrder = spriteRenderer.sortingOrder - 1; // behind the player

        ghost.AddComponent<AfterImage>().Init(ghostLifetime, ghostColor);
    }

}