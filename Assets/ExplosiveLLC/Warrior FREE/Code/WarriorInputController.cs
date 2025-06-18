using UnityEngine;

namespace WarriorAnim
{
    public class WarriorInputController : MonoBehaviour
    {
        [HideInInspector] public bool inputAttack;
        [HideInInspector] public float inputHorizontal = 0;
        [HideInInspector] public float inputVertical = 0;

        public Vector3 moveInput { get { return CameraRelativeInput(inputHorizontal, inputVertical); } }

        private void Update()
        {
            Inputs();
            Toggles();
        }

        private void Inputs()
        {
            inputAttack = Input.GetButtonDown("Attack");
            inputHorizontal = Input.GetAxisRaw("Horizontal");
            inputVertical = Input.GetAxisRaw("Vertical");
        }

        private void Toggles()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (Time.timeScale != 1) { Time.timeScale = 1; }
                else { Time.timeScale = 0.125f; }
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (Time.timeScale != 1) { Time.timeScale = 1; }
                else { Time.timeScale = 0f; }
            }
        }

        private Vector3 CameraRelativeInput(float inputX, float inputZ)
        {
            Vector3 forward = Camera.main.transform.TransformDirection(Vector3.forward);
            forward.y = 0;
            forward = forward.normalized;

            Vector3 right = new Vector3(forward.z, 0, -forward.x);
            Vector3 relativeVelocity = inputHorizontal * right + inputVertical * forward;

            if (relativeVelocity.magnitude > 1) { relativeVelocity.Normalize(); }

            return relativeVelocity;
        }
    }
}
