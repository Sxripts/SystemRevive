using System;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.Design.AxImporter;

namespace SystemMaintenance
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        static readonly string logFilePath = Path.Combine(Path.GetTempPath(), "systemrevive-logs.txt");

        static void Main()
        {
            Console.BufferWidth = 1000;
            Console.BufferHeight = 1000;

            int defaultWidth = 70;
            int defaultHeight = 20;

            // Set the default window size
            SetConsoleSize(70, 20);

            // Get the screen size
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            while (true)
            {
                try
                {
                    if (IsAdministrator())
                    {
                        Console.WriteLine("Running with administrator privileges.");
                    }
                    else { }

                    Console.WriteLine("Choose an option:");
                    Console.WriteLine("1. Flush DNS");
                    Console.WriteLine("2. Renew IP");
                    Console.WriteLine("3. Clean %Temp%");
                    Console.WriteLine("4. Clean Windows Update Cache");
                    Console.WriteLine("5. Clean Unnecessary System Files (Admin Privileges)");
                    Console.WriteLine("6. Look at all the applications in startup");

                    if (!int.TryParse(Console.ReadLine(), out int option))
                    {
                        Console.WriteLine("Invalid input! Please enter a number.");
                        continue;
                    }

                    switch (option)
                    {
                        case 1:
                            Console.Clear();
                            SetConsoleSize(170, 40);
                            FlushDNS();
                            break;
                        case 2:
                            Console.Clear();
                            SetConsoleSize(170, 40);
                            RenewIP();
                            break;
                        case 3:
                            Console.Clear();
                            SetConsoleSize(170, 40);
                            _ = CleanTemp();
                            break;
                        case 4:
                            Console.Clear();
                            SetConsoleSize(170, 40);
                            CleanWindowsUpdateCache();
                            break;
                        case 5:
                            if (IsAdministrator())
                            {
                                Console.Clear();
                                SetConsoleSize(170, 40);
                                CleanUnnecessarySystemFiles();
                            }
                            else
                            {
                                Console.Clear();
                                LogAndPrint("This action requires administrator privileges. Please restart the application as an administrator.");
                            }
                            break;
                        case 6:
                            Console.Clear();
                            SetConsoleSize(170, 40);
                            LookAtStartupApps();
                            break;
                        default:
                            Console.WriteLine("Invalid option!");
                            SetConsoleSize(170, 40);
                            Thread.Sleep(2000);
                            Console.Clear();
                            break;
                    }
                    Console.WindowWidth = defaultWidth;
                    Console.WindowHeight = defaultHeight;
                }
                catch (Exception ex)
                {
                    LogAndPrint($"An unexpected error occurred: {ex.Message}");
                }
            }

            static void SetConsoleSize(int width, int height)
            {
                IntPtr handle = GetConsoleWindow();

                int fontWidth = 8;
                int fontHeight = 12;
                int windowWidth = width * fontWidth;
                int windowHeight = height * fontHeight;

                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                // Center the window on the screen
                int x = (screenWidth - windowWidth) / 2;
                int y = (screenHeight - windowHeight) / 2;

                // Resize and move the window
                MoveWindow(handle, x, y, windowWidth, windowHeight, true);
            }

            static async void FlushDNS()
            {
                Process.Start(new ProcessStartInfo("ipconfig", "/flushdns") { CreateNoWindow = true });
                Console.Clear();
                Console.WriteLine("DNS flushed successfully!");
                await Task.Delay(2000);
                Console.Clear();

                DisplayNavigationOptions();
            }

            static async void RenewIP()
            {
                Process.Start(new ProcessStartInfo("ipconfig", "/renew") { CreateNoWindow = true });
                Console.Clear();
                Console.WriteLine("IP renewed successfully!");
                await Task.Delay(2000);
                Console.Clear();

                DisplayNavigationOptions();
            }

            static async Task CleanTemp()
            {
                string tempPath = Path.GetTempPath();
                DirectoryInfo di = new(tempPath);

                var tasks = new List<Task>();

                foreach (FileInfo file in di.GetFiles())
                {
                    if (file.Name.Equals("systemrevive-logs.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Deleting files asynchronously
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (IOException)
                        {
                            // Logging or any specific action on a failure to delete due to an IOException
                        }
                    }));
                }

                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            dir.Delete(true);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Logging or any specific action on a failure to delete due to an UnauthorizedAccessException
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                Console.Clear();
                Console.WriteLine("%Temp% cleaned successfully!");
                await Task.Delay(2000);
                Console.Clear();

                DisplayNavigationOptions();
            }

            static void CleanWindowsUpdateCache()
            {
                Console.WriteLine("Warning: This action may disrupt ongoing Windows Updates.");
                Console.WriteLine("Do you want to proceed? (Y/N)");
                string response = Console.ReadLine();

                if (response.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    StopWindowsServices();
                    StartWindowsServices();

                    Console.WriteLine("Windows Update Cache cleaned successfully!");
                }
                else
                {
                    Console.WriteLine("Action canceled by user.");
                }
            }

            static void StopWindowsServices()
            {
                string[] services = { "wuauserv", "cryptSvc", "bits", "msiserver" };
                foreach (string service in services)
                {
                    Process.Start(new ProcessStartInfo("net", "stop " + service) { CreateNoWindow = true }).WaitForExit();
                }
            }

            static void StartWindowsServices()
            {
                string[] services = { "wuauserv", "cryptSvc", "bits", "msiserver" };
                foreach (string service in services)
                {
                    Process.Start(new ProcessStartInfo("net", "start " + service) { CreateNoWindow = true });
                }
            }

            static void CleanUnnecessarySystemFiles()
            {
                CleanFolder(@"C:\ProgramData\Microsoft\Windows\WER", "Windows Error Reporting Files");
                CleanFolder(@"C:\Windows.old", "Old Windows Installation Files");
                LogAndPrint("Unnecessary system files cleaned successfully!");
            }

            static void CleanFolder(string path, string description)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        LogAndPrint($"Cleaning {description}...");
                        DirectoryInfo di = new(path);

                        FileInfo[] files = di.GetFiles();
                        DirectoryInfo[] directories = di.GetDirectories();
                        int totalItems = files.Length + directories.Length;
                        int processedItems = 0;

                        foreach (FileInfo file in files)
                        {
                            file.Delete();
                            processedItems++;
                            ShowProgress(processedItems, totalItems);
                        }

                        foreach (DirectoryInfo dir in directories)
                        {
                            dir.Delete(true);
                            processedItems++;
                            ShowProgress(processedItems, totalItems);
                        }
                    }
                    else
                    {
                        LogAndPrint($"Skipping {description} (path not found).");
                    }
                }
                catch (Exception ex)
                {
                    LogAndPrint($"An error occurred while cleaning {description}: {ex.Message}");
                }
            }

            static void PrintCenteredTitle(string title)
            {
                Console.WriteLine(new string('=', Console.WindowWidth - 1));
                Console.WriteLine(title.PadLeft((Console.WindowWidth + title.Length) / 2).PadRight(Console.WindowWidth - 1));
                Console.WriteLine(new string('=', Console.WindowWidth - 1));
            }

            static void LookAtStartupApps()
            {
                string[][] registryPaths = {
        new[] { @"Software\Microsoft\Windows\CurrentVersion\Run",
                @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce" },

        new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce" }
            };

                PrintCenteredTitle("Startup Applications from HKEY_CURRENT_USER Registry");
                PrintStartupAppsFromRegistry(registryPaths[0], Registry.CurrentUser);
                Console.WriteLine(new string('-', Console.WindowWidth - 1)); // Divider line

                PrintCenteredTitle("Startup Applications from HKEY_LOCAL_MACHINE Registry");
                PrintStartupAppsFromRegistry(registryPaths[1], Registry.LocalMachine);
                Console.WriteLine(new string('-', Console.WindowWidth - 1)); // Divider line

                PrintCenteredTitle("Startup Applications from Startup Folder");
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                foreach (string file in Directory.GetFiles(startupFolder))
                {
                    Console.WriteLine(Path.GetFileName(file));
                }

                Console.WriteLine(new string(' ', Console.WindowWidth - 1)); // End line with space instead of '-'

                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Go back");
                Console.WriteLine("2. Exit");

                if (!int.TryParse(Console.ReadLine(), out int navigationChoice))
                {
                    Console.WriteLine("Invalid input! Please enter a number.");
                }

                switch (navigationChoice)
                {
                    case 1:
                        Console.Clear();
                        return; // Go back to the main menu
                    case 2:
                        Environment.Exit(0); // Exit the program
                        break;
                    default:
                        Console.WriteLine("Invalid option!");
                        Thread.Sleep(2000);
                        Console.Clear();
                        break;
                }
            }

            static void PrintStartupAppsFromRegistry(string[] paths, RegistryKey rootKey)
            {
                foreach (string path in paths)
                {
                    using RegistryKey key = rootKey.OpenSubKey(path);
                    if (key != null)
                    {
                        foreach (string appName in key.GetValueNames())
                        {
                            if (appName.StartsWith("MicrosoftEdgeAutoLaunch_") || appName.StartsWith("GoogleChromeAutoLaunch_"))
                            {
                                continue;
                            }

                            string value = key.GetValue(appName).ToString();
                            string executablePath = value.Split(' ')[0].Trim('"');

                            Console.WriteLine($"{appName.Replace('-', ' '),-50} : {executablePath}");
                        }
                    }
                }
            }


            static void DisplayNavigationOptions()
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Go back");
                Console.WriteLine("2. Exit");

                if (!int.TryParse(Console.ReadLine(), out int navigationChoice))
                {
                    Console.WriteLine("Invalid input! Please enter a number.");
                }

                switch (navigationChoice)
                {
                    case 1:
                        // Code to go back to the main menu
                        break;
                    case 2:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Invalid option!");
                        break;
                }
            }

            static void ShowProgress(int completed, int total)
            {
                int percent = (int)((double)completed / total * 100);
                int numberOfBars = percent / 5;

                Console.Write("\r[");
                Console.Write(new string('▰', numberOfBars));
                Console.Write(new string(' ', 20 - numberOfBars));
                Console.Write($"] {percent}%");
                if (completed == total)
                {
                    Console.WriteLine();
                }
            }

            static void LogAndPrint(string message)
            {
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                Console.WriteLine(message);
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }

            static bool IsAdministrator()
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}