namespace UIKit.StateMachine {
    public interface IUIState {
        UIStateId Id { get; }
        void Enter();
        void Exit();
    }
}