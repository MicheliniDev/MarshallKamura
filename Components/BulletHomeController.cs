using UnityEngine;

namespace KamuraPrime
{
    public class BulletHomeController : MonoBehaviour
    {
        public Transform target;
        public Rigidbody2D rb;
        public float turnSpeed = 200f; 
        public float moveSpeed;
        public bool isActive = false;

        public void OnDisable()
        {
            isActive = false;
        }

        public void FixedUpdate()
        {
            if (!isActive || target == null || rb == null || rb.isKinematic) return;

            Vector2 direction = (Vector2)target.position - rb.position;
            direction.Normalize();

            float rotateAmount = Vector3.Cross(direction, rb.velocity.normalized).z;

            rb.angularVelocity = -rotateAmount * turnSpeed;
            rb.velocity = transform.right * moveSpeed;
        }

        public void Initialize(Rigidbody2D rb, float moveSpeed, float turnSpeed, Transform target)
        {
            this.rb = rb;
            this.target = target;
            this.moveSpeed = moveSpeed;
            this.turnSpeed = turnSpeed;
            isActive = true;
        }
    }
}
