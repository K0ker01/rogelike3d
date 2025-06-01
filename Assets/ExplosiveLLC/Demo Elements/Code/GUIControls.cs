using UnityEngine;

namespace WarriorAnim
{
    public class GUIControls : MonoBehaviour
    {
        private WarriorController warriorController;

        private void Awake()
        {
            warriorController = GetComponent<WarriorController>();
        }

        private void OnGUI()
        {
            if (warriorController.canAction)
            {
                Attacking();
            }

            Debug();
        }

        private void Attacking()
        {
            if (warriorController.MaintainingGround() && warriorController.canAction)
            {
                if (GUI.Button(new Rect(25, 85, 100, 30), "Attack1"))
                {
                    warriorController.Attack1();
                }
            }
        }

        private void Debug()
        {
            if (GUI.Button(new Rect(600, 15, 120, 30), "Debug Controller"))
            {
                warriorController.ControllerDebug();
            }
            if (GUI.Button(new Rect(600, 50, 120, 30), "Debug Animator"))
            {
                warriorController.AnimatorDebug();
            }
        }
    }
}
