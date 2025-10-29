using UnityEngine;

public class GlobalGameControls : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool quitOnEscape = true;

    [Header("Testing (optional)")]
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.F1;

    void Start()
    {
        if (lockCursorOnStart) LockCursor(true);
    }

    void Update()
    {
        if (quitOnEscape && Input.GetKeyDown(KeyCode.Escape))
            Quit(); // Behavior differs in Editor vs Build

        if (toggleCursorKey != KeyCode.None && Input.GetKeyDown(toggleCursorKey))
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        // In the Editor: only stop Play Mode if Shift is held. ESC alone is ignored.
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            Debug.Log("Editor: Shift+ESC -> Stop Play Mode");
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
        {
            Debug.Log("Editor: ESC ignored (use Shift+ESC to stop Play)");
        }
#else
        // In a Build: ESC quits the application.
        Debug.Log("Quit requested (Build)");
        Application.Quit();
#endif
    }
}

