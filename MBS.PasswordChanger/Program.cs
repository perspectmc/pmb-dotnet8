using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Web.Security;

namespace MBS.PasswordChanger
{
    static class DisableConsoleQuickEdit
    {

        const uint ENABLE_QUICK_EDIT = 0x0040;

        // STD_INPUT_HANDLE (DWORD): -10 is the standard input device.
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        internal static bool Go()
        {

            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                // ERROR: Unable to get console mode.
                return false;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT;

            // set the new mode
            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                // ERROR: Unable to set console mode
                return false;
            }

            return true;
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            DisableConsoleQuickEdit.Go();

            var program = new Program();
            program.ChangePassowrd();

        }

        private void ChangePassowrd()
        {
            Console.WriteLine("******************************************");
            Console.WriteLine("This console is used to change the password for the admin account!!!");
            Console.WriteLine("The password must meet the following conditions");
            Console.WriteLine("- Must be at least 7 characters");
            Console.WriteLine("- Must have at least 1 digit");
            Console.WriteLine("- Must have at least 1 special (non-alphanumeric) character");
            Console.WriteLine("******************************************");
            Console.WriteLine("");

            Console.WriteLine("Enter New Passord for admin account: ");
            var newPassword = Console.ReadLine().Trim();

            var adminUserId = new Guid(ConfigurationManager.AppSettings["AdminUserId"]);

            var user = Membership.GetUser((Guid) adminUserId);

            var resetPassword = user.ResetPassword();

            Console.WriteLine("Changing Password now");

            if (!user.ChangePassword(resetPassword, newPassword))
            {
                Console.WriteLine("ERROR: Unable to change the password, please try again!");
            }
            else
            {
                Console.WriteLine("Password change successfully!");
            }

            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();
        }
    }
}
