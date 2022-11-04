using UnityEngine;
using UnityEngine.InputSystem;

public class ExamplePlayerInput : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;

    private Vector2 controlMove;
    private float cameraPitch;

    #region Input Message Handlers
    public void OnInputMove(InputValue value) => controlMove = value.Get<Vector2>();
    public void OnInputAim(InputValue value) => ApplyAimDelta(value.Get<Vector2>());
    #endregion

    private void ApplyAimDelta(Vector2 delta)
    {
        // horizontal
        // applied to player (our own) transform
        transform.Rotate(Vector3.up, delta.x);

        // vertical
        // applied to camera transform
        // euler angles are generally a BadIdea™, but for this simple example it's fine
        // it does mean we need 'cameraPitch' though, bit harder to read back the pitch from the camera transform robustly
        cameraPitch -= delta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0, 0);
    }

    private void Update()
    {
        // calculate world space velocity
        Vector3 velocity = Vector3.zero;
        velocity += controlMove.x * transform.right;
        velocity += controlMove.y * transform.forward;

        // calculate new position and clamp it
        Vector3 newPosition = transform.position + velocity * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, 0f, 10f);
        newPosition.y = Mathf.Clamp(newPosition.y, 0f, 10f);

        // apply new position to transform
        transform.position = newPosition;
    }
}
