using System.Collections.Generic;

namespace UIKit.StateMachine {
    public class UIStateMachine {
        private readonly Dictionary<UIStateId, IUIState> _states = new();
        private readonly Stack<IUIState> _stack = new();

        public IUIState Current => _stack.Count > 0 ? _stack.Peek() : null;

        public void Register(IUIState state) {
            _states[state.Id] = state;
        }

        public void Push(UIStateId id) {
            if (!_states.TryGetValue(id, out var next))
                return;

            if (Current != null)
                Current.Exit();
            _stack.Push(next);
            next.Enter();
        }

        public void Pop() {
            if (_stack.Count == 0)
                return;

            var top = _stack.Pop();
            top.Exit();

            if (Current != null)
                Current.Enter();
        }

        public void Switch(UIStateId id) {
            // 清栈后进入目标状态（用于回到 Menu）
            while (_stack.Count > 0) {
                var s = _stack.Pop();
                s.Exit();
            }
            Push(id);
        }

        public bool Is(UIStateId id) => Current != null && Current.Id == id;
    }
}