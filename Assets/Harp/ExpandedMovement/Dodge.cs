using SwiftKraft.Gameplay.Common.FPS;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS
{
    [RequireComponent(typeof(FPSCharacterControllerMotor))]
    public class Dodge : MonoBehaviour
    {
        private FPSCharacterControllerMotor motor;

        public float dodgeDistance = 5f; 
        public float dodgeDuration = 0.2f;
        private bool isDodging = false;


        
        private void Awake()
        {
            motor = GetComponent<FPSCharacterControllerMotor>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt) && !isDodging)
            {
                AttemptDodge();
            }
        }

        private void AttemptDodge()
        {
            if (motor.IsGrounded)
            {
                Vector2 inputMove = motor.GetInputMove();
                if (inputMove.sqrMagnitude > 0f)
                {
                    
                    StartCoroutine(DodgeCoroutine(inputMove));
                }
            }
        }

        private System.Collections.IEnumerator DodgeCoroutine(Vector2 inputMove)
        {
            isDodging = true;
            motor.MoveSpeed = 2;

            // Calculate the dodge direction
            Vector3 dodgeDirection = new Vector3(inputMove.x, 0f, inputMove.y).normalized;
            // Ensure we have a valid direction to dodge
            if (dodgeDirection == Vector3.zero)
            {
                isDodging = false; // End the dodge if no direction is provided
                yield break;
            }

            // Get the CharacterController component
            CharacterController characterController = GetComponent<CharacterController>();
            // Calculate the dodge velocity
            Vector3 dodgeVelocity = transform.rotation * dodgeDirection * (dodgeDistance / dodgeDuration);

            float elapsedTime = 0f;

            while (elapsedTime < dodgeDuration)
            {
                // Move the character controller
                characterController.Move(dodgeVelocity * Time.deltaTime);
                elapsedTime += Time.deltaTime;
                yield return null; // Wait for the next frame
            }

            motor.MoveSpeed = 5;
            isDodging = false; // End the dodge
        }
    }
}
