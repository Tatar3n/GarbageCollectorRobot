using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Move : MonoBehaviour
{
    private Rigidbody2D _rigidbody;
    private BoxCollider2D _boxCollider;
    private Vector2 _horizontalVelocity; 
    private float _horizontalSpeed; 
    private Vector2 _verticalVelocity; 
    private float _verticalSpeed; 
    public float moveSpeed; 

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _rigidbody.gravityScale = 0; 
    }

    void Update()
    {
        _rigidbody.constraints = RigidbodyConstraints2D.None;
        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        _horizontalSpeed = Input.GetAxis("Horizontal");
        _verticalSpeed = Input.GetAxis("Vertical");
    }

    private void FixedUpdate()
    {
        Step();
    }

    private void Step()
    {
        _horizontalVelocity.Set(_horizontalSpeed * moveSpeed, _rigidbody.velocity.y);
        _verticalVelocity.Set(_rigidbody.velocity.x, _verticalSpeed * moveSpeed);
        _rigidbody.velocity = new Vector2(_horizontalVelocity.x, _verticalVelocity.y);
    }
}