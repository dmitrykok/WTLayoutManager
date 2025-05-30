using System.Windows.Input;

namespace WTLayoutManager.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The can execute logic. Defaults to null.</param>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can be executed with the given parameter.
        /// </summary>
        /// <param name="parameter">The parameter to use when determining whether the command can be executed.</param>
        /// <returns>true if the command can be executed; otherwise, false.</returns>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>
        /// Executes the command with the given parameter.
        /// </summary>
        /// <param name="parameter">The parameter to use when executing the command.</param>
        public void Execute(object? parameter) => _execute(parameter);

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value!;
            remove => CommandManager.RequerySuggested -= value!;
        }
    }
}
