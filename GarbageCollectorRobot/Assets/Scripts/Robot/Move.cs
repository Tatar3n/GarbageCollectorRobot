using UnityEngine;

public class Move : MonoBehaviour
{
    private Rigidbody2D _rigidbody;
    private float _horizontalSpeed;
    private float _verticalSpeed;

    public float moveSpeed;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();

        // Если на объекте есть ИИ-движение, не конфликтуем с ним.
        var ai = GetComponent<Fuzzy.FuzzyLogicSystem>();
        if (ai != null && ai.enabled)
        {
            enabled = false;
            return;
        }

        if (_rigidbody != null)
        {
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    void Update()
    {
        _horizontalSpeed = Input.GetAxis("Horizontal");
        _verticalSpeed = Input.GetAxis("Vertical");
    }

    private void FixedUpdate()
    {
        Step();
    }

    private void Step()
    {
        if (_rigidbody == null) return;
        _rigidbody.velocity = new Vector2(_horizontalSpeed * moveSpeed, _verticalSpeed * moveSpeed);
    }
}