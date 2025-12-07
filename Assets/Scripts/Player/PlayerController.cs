using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

public enum PlayerGameState
{
    Default, // Initial state when player joins the game (not ready)
    Ready, // The game can only start when all players are in Ready state
    Playing, // The player can move and interact in the game
    Ended, // When the player has been caught or finished the game
}

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public float hp;
    public float maxHP = 6f;
    public int playerIndex;
    public float MoveSpeed = 10f;
    public CinemachineCamera cinemachineCamera;

    [HideInInspector]
    public bool canMove = true;

    [HideInInspector]
    public Vector3 spawnPoint;

    [HideInInspector]
    public UnityEvent<float> OnPlayerDamaged;

    protected Rigidbody _rb;

    [HideInInspector]
    public Animator _animator;

    Coroutine currentDamageCoroutine;
    Renderer[] renderers;
    Color defaultColor;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Start()
    {
        hp = maxHP;

        spawnPoint = transform.position;

        renderers = GetComponentsInChildren<Renderer>();

        // ignore VFXRenderers for color change
        renderers = System.Array.FindAll(renderers, rend => !(rend is VFXRenderer));
        defaultColor = renderers[0].material.color;
    }

    protected virtual void Update() { }

    protected virtual void FixedUpdate() { }

    public virtual void TakeDamage(float damage)
    {
        if (currentDamageCoroutine != null)
        {
            StopCoroutine(currentDamageCoroutine);
        }

        StartCoroutine(DamageCoroutine(Color.red, damage));
    }

    protected IEnumerator DamageCoroutine(Color damagedColor, float damage)
    {
        hp = Mathf.Clamp(hp - damage, 0, maxHP);
        OnPlayerDamaged?.Invoke(hp);
        StartCoroutine(DamageVisualCoroutine());
        yield return new WaitForSeconds(1f);

        if (!_rb.isKinematic)
            _rb.linearVelocity = Vector3.zero;

        IEnumerator DamageVisualCoroutine()
        {
            // Flash to damaged color then go back to default color after 0.2f seconds, ignoring VFXRenderers
            foreach (Renderer rend in renderers)
            {
                if (rend is VFXRenderer)
                    continue;
                rend.material.color = damagedColor;
            }
            yield return new WaitForSeconds(0.2f);
            foreach (Renderer rend in renderers)
            {
                if (rend is VFXRenderer)
                    continue;
                rend.material.color = defaultColor;
            }
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (
            collision.gameObject.layer == LayerMask.NameToLayer("TransparentFX")
            && collision.gameObject.TryGetComponent(out Rigidbody rb)
            && !collision.gameObject.TryGetComponent(out Destructible _)
        )
        {
            rb.isKinematic = false;
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Respawn"))
        {
            transform.position = spawnPoint;
            _rb.linearVelocity = Vector3.zero;
        }
    }
}
