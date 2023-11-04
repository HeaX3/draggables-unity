using UnityEngine;
using UnityEngine.InputSystem;

namespace Draggables
{
    public class DraggablesInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputMaster;

        public static InputAction pointAction;
        public static InputAction interactAction;

        private void Awake()
        {
            pointAction = inputMaster.FindAction("Point");
            interactAction = inputMaster.FindAction("Interact");
            if (!pointAction.enabled) pointAction.Enable();
            if (!interactAction.enabled) interactAction.Enable();
        }
    }
}