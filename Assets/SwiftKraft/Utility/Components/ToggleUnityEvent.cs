using UnityEngine;
using UnityEngine.Events;

public class ToggleUnityEvent : MonoBehaviour
{
    public bool DefaultState;
    public bool CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState == value)
                return;

            _currentState = value;
            (value ? True : False)?.Invoke();
        }
    }

    public UnityEvent True;
    public UnityEvent False;

    bool _currentState;

    protected virtual void Start()
    {
        _currentState = !DefaultState;
        CurrentState = DefaultState;
    }

    public void Toggle() => CurrentState = !CurrentState;
}
