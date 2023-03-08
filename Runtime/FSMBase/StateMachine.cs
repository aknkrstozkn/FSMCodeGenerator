using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FSM
{
    public class StateMachine<TOwner, TBaseState, TStateType> where TBaseState : BaseState<TOwner, TStateType> where TOwner : MonoBehaviour where TStateType : StateType<TStateType>
    {
        private Dictionary<int, TBaseState> _states = new Dictionary<int, TBaseState>();
        public Dictionary<int, TBaseState> States => _states;
        
        private TBaseState _currentState;
        public TBaseState CurrentState => _currentState;

        public void Init(TStateType startStateType, TBaseState[] states)
        {
            AddStates(states);
            InitStartState(startStateType);
        }

        private void InitStartState(TStateType startStateType)
        {
            _currentState = _states[startStateType.Index];
            _currentState.RaiseEvent(StateEventType.PreEnter);
            _currentState.Enter();
            _currentState.RaiseEvent(StateEventType.PostEnter);
        }
        
        private void AddStates(TBaseState[] states)
        {
            foreach (var state in states)
            {
                if (_states.ContainsKey(state.type.Index))
                {
#if UNITY_EDITOR
                    Debug.LogError("State Machine should not contain same index of states or states with same types", Object.FindObjectOfType<TOwner>());
#endif
                    return;
                }
                
                _states.Add(state.type.Index, state);
            }
        }
        
        public void ChangeState(TStateType type)
        {
            if (!_states.TryGetValue(type.Index, out TBaseState state))
            {
#if UNITY_EDITOR
                Debug.LogError("Can't change state, because state does not exist!", Object.FindObjectOfType<TOwner>());
#endif
                return;
            }

            ExitCurrentState();
            _currentState = state;
            EnterCurrentState();
        }

        private void ExitCurrentState()
        {
            _currentState.RaiseEvent(StateEventType.PreExit);
            _currentState.Exit();
            _currentState.RaiseEvent(StateEventType.PostExit);
        }

        private void EnterCurrentState()
        {
            _currentState.RaiseEvent(StateEventType.PreEnter);
            _currentState.Enter();
            _currentState.RaiseEvent(StateEventType.PostEnter);
        }

        public void SignOutStateEvent(Action<TOwner> action, TStateType stateType, StateEventType stateEventType = StateEventType.PostEnter)
        {
            if (!_states.ContainsKey(stateType.Index))
            {
#if UNITY_EDITOR
                Debug.LogError("Can't sign out from state event, because state does not exist!", Object.FindObjectOfType<TOwner>());
#endif
                return;
            }
            _states[stateType.Index].SignOutEvent(stateEventType, action);
        }

        public void SignUpStateEvent(Action<TOwner> action, TStateType stateType, StateEventType stateEventType = StateEventType.PostEnter)
        {
            if (!_states.ContainsKey(stateType.Index))
            {
#if UNITY_EDITOR
                Debug.LogError("Can't sign up to state event, because state does not exist!", Object.FindObjectOfType<TOwner>());
#endif
                return;
            }
            _states[stateType.Index].SignUpEvent(stateEventType, action);
        }
    }
}
