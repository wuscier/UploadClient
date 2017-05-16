using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace UploadClient
{
    public class EventArgsCommand : TriggerAction<DependencyObject>
    {
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(EventArgsCommand), new PropertyMetadata(null));


        public object CommandParameter
        {
            get { return (object)GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommandParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(EventArgsCommand), new PropertyMetadata(null));


        protected override void Invoke(object parameter)
        {
            if (CommandParameter != null)
            {
                parameter = CommandParameter;
            }

            var cmd = Command;
            if (cmd != null)
            {
                cmd.Execute(parameter);
            }
        }
    }
}
