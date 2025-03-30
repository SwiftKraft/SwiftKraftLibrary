using UnityEngine;

public class FPS_Limiter : MonoBehaviour
{
    public int targetFPS;

    void Start()
    {
        Application.targetFrameRate = targetFPS;
    }
}
