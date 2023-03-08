using System;
using UnityEngine;
namespace FSM
{
    public enum StateEventType
    {
        PreExit,
        PostExit,
        PreEnter,
        PostEnter
    }
    
    public abstract class BaseState<TOwner, TStateType> where TOwner : MonoBehaviour where TStateType : StateType<TStateType>
    {
        private Action<TOwner> _preExitEvent;
        private Action<TOwner> _postExitEvent;

        private Action<TOwner> _preEnterEvent;
        private Action<TOwner> _postEnterEvent;

        public TStateType type;
        protected readonly TOwner owner;
        
        protected BaseState(TOwner owner, TStateType type)
        {
            this.owner = owner;
            this.type = type;
        }

        public abstract void Enter();

        public abstract void Exit();
        
        public void RaiseEvent(StateEventType eventType)
        {
            switch (eventType)
            {
                case StateEventType.PreExit:
                    _preExitEvent?.Invoke(owner);
                    break;
                case StateEventType.PostExit:
                    _postExitEvent?.Invoke(owner);
                    break;
                case StateEventType.PreEnter:
                    _preEnterEvent?.Invoke(owner);
                    break;
                case StateEventType.PostEnter:
                    _postEnterEvent?.Invoke(owner);
                    break;
                default:
#if UNITY_EDITOR
                    Debug.LogError($"Base State doesn't have {eventType.ToString()} type of event type!", owner);
#endif
                    break;

            }
        }
        
        public void SignUpEvent(StateEventType eventType, Action<TOwner> onEvent)
        {
            switch (eventType)
            {
                case StateEventType.PreExit:
                    _preExitEvent += onEvent;
                    break;
                case StateEventType.PostExit:
                    _postExitEvent += onEvent;
                    break;
                case StateEventType.PreEnter:
                    _preEnterEvent += onEvent;
                    break;
                case StateEventType.PostEnter:
                    _postEnterEvent += onEvent;
                    break;
                default:
#if UNITY_EDITOR
                    Debug.LogError($"Base State doesn't have {eventType.ToString()} type of event type!", owner);
#endif
                    break;

            }
        }
        
        public void SignOutEvent(StateEventType eventType, Action<TOwner> onEvent)
        {
            switch (eventType)
            {
                case StateEventType.PreExit:
                    _preExitEvent -= onEvent;
                    break;
                case StateEventType.PostExit:
                    _postExitEvent -= onEvent;
                    break;
                case StateEventType.PreEnter:
                    _preEnterEvent -= onEvent;
                    break;
                case StateEventType.PostEnter:
                    _postEnterEvent -= onEvent;
                    break;
                default:
#if UNITY_EDITOR
                    Debug.LogError($"Base State doesn't have {eventType.ToString()} type of event type!", owner);
#endif
                    break;

            }
        }
    }
}
