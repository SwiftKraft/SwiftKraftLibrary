using UnityEngine;


namespace Beautify.Universal {

    public class SphereAnimator : MonoBehaviour {

        Rigidbody rb;
        const float SPEED = 4;

        void Start () {
            rb = GetComponent<Rigidbody>();
            Application.targetFrameRate = 60;
        }

        void FixedUpdate () {
            if (transform.position.z < 0.5f) {
                rb.velocity = Vector3.forward * SPEED;
            }
            else if (transform.position.z > 8f) {
                rb.velocity = Vector3.back * SPEED;
            }
        }

    }
}