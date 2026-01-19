using Microsoft.Win32;

namespace Brute_Force_password_cracker.Services
{
    public class FileDialogService
    {
        public virtual string ShowOpenFileDialog(string filter, string title = null)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
