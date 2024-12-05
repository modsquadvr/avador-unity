using System;

namespace UPP.Utils.State
{
    public interface IStateMachine<in TEnum> : IState where TEnum : Enum
    {
        void AddState(TEnum state_id, IState state);
        void AddSubStateMachine<TSubEnum>(TEnum state_id, IStateMachine<TSubEnum> child) where TSubEnum : Enum;

        void RemoveState(TEnum state_id);
        void RemoveSubStateMachine(TEnum state_id);
        void TransitionTo(TEnum new_state_id);

        void Init(TEnum initial_state_id);
    }
}