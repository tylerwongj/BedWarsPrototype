using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.IsValid())
            {
                SceneManager.LoadScene(currentScene.buildIndex);
            }
        }
    }
}
