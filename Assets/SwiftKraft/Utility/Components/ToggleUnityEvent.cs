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
            Dynamic?.Invoke(_currentState);
            Inverted?.Invoke(!_currentState);
        }
    }

    public UnityEvent True;
    public UnityEvent False;
    public UnityEvent<bool> Dynamic;
    public UnityEvent<bool> Inverted;

    bool _currentState;

    protected virtual void Start()
    {
        _currentState = !DefaultState;
        CurrentState = DefaultState;
    }

    public void Toggle() => CurrentState = !CurrentState;
}
