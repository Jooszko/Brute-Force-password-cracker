using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;

namespace Brute_Force_password_cracker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}