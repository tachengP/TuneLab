using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TuneLab.Base.Utils;

namespace TuneLab.Utils;

internal static class FileAssociation
{
    private const string FileExtension = ".tlp";
    private const string FileTypeId = "TuneLab.Project";
    private const string FileDescriptionEn = "TuneLab Project";
    private const string FileDescriptionCn = "TuneLab 工程文件";

    public static void RegisterFileAssociation()
    {
        try
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
            {
                Log.Error("Failed to get executable path for file association");
                return;
            }

            var appIconPath = Path.Combine(PathManager.ExcutableFolder, "Assets", "app.ico");
            var fileIconPath = Path.Combine(PathManager.ExcutableFolder, "Assets", "file.ico");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RegisterWindowsAssociation(executablePath, fileIconPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                RegisterLinuxAssociation(executablePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                RegisterMacOSAssociation(executablePath);
            }

            Log.Info("File association registered successfully");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to register file association: {ex}");
        }
    }

    private static void RegisterWindowsAssociation(string executablePath, string fileIconPath)
    {
        try
        {
            // Register the file extension
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{FileExtension}"))
            {
                key?.SetValue("", FileTypeId);
            }

            // Register the file type with icon and description
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{FileTypeId}"))
            {
                key?.SetValue("", FileDescriptionEn);
            }

            // Set the icon
            if (File.Exists(fileIconPath))
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{FileTypeId}\DefaultIcon"))
                {
                    key?.SetValue("", $"\"{fileIconPath}\",0");
                }
            }

            // Register the open command
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($@"Software\Classes\{FileTypeId}\shell\open\command"))
            {
                key?.SetValue("", $"\"{executablePath}\" \"%1\"");
            }

            // Notify the system about the file association change
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to register Windows file association: {ex}");
        }
    }

    private static void RegisterLinuxAssociation(string executablePath)
    {
        try
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var desktopDir = Path.Combine(homeDir, ".local", "share", "applications");
            PathManager.MakeSureExist(desktopDir);

            var desktopFilePath = Path.Combine(desktopDir, "tunelab.desktop");
            var iconPath = Path.Combine(PathManager.ExcutableFolder, "Assets", "app.png");
            var fileIconPath = Path.Combine(PathManager.ExcutableFolder, "Assets", "file.png");

            // Create .desktop file
            var desktopFileContent = $@"[Desktop Entry]
Type=Application
Name=TuneLab
Comment={FileDescriptionEn}
Exec=""{executablePath}"" %f
Icon={iconPath}
MimeType=application/x-tunelab-project;
Categories=AudioVideo;Audio;
Terminal=false
";

            File.WriteAllText(desktopFilePath, desktopFileContent);

            // Create MIME type definition
            var mimeDir = Path.Combine(homeDir, ".local", "share", "mime", "packages");
            PathManager.MakeSureExist(mimeDir);

            var mimeFilePath = Path.Combine(mimeDir, "tunelab.xml");
            var mimeFileContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<mime-info xmlns=""http://www.freedesktop.org/standards/shared-mime-info"">
    <mime-type type=""application/x-tunelab-project"">
        <comment>{FileDescriptionEn}</comment>
        <comment xml:lang=""zh_CN"">{FileDescriptionCn}</comment>
        <glob pattern=""*{FileExtension}""/>
        <icon name=""tunelab-file""/>
    </mime-type>
</mime-info>
";

            File.WriteAllText(mimeFilePath, mimeFileContent);

            // Install icon for file type
            var iconThemeDir = Path.Combine(homeDir, ".local", "share", "icons", "hicolor", "256x256", "mimetypes");
            PathManager.MakeSureExist(iconThemeDir);
            
            var targetIconPath = Path.Combine(iconThemeDir, "tunelab-file.png");
            if (File.Exists(fileIconPath))
            {
                File.Copy(fileIconPath, targetIconPath, true);
            }

            // Update MIME database
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "update-mime-database",
                    Arguments = Path.Combine(homeDir, ".local", "share", "mime"),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(5000);
            }
            catch { /* Ignore if command doesn't exist */ }

            // Update desktop database
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "update-desktop-database",
                    Arguments = desktopDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(5000);
            }
            catch { /* Ignore if command doesn't exist */ }

            // Update icon cache
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "gtk-update-icon-cache",
                    Arguments = $"-f -t {Path.Combine(homeDir, ".local", "share", "icons", "hicolor")}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(5000);
            }
            catch { /* Ignore if command doesn't exist */ }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to register Linux file association: {ex}");
        }
    }

    private static void RegisterMacOSAssociation(string executablePath)
    {
        try
        {
            // On macOS, file associations are typically handled by Info.plist in the app bundle
            // Since this is a runtime registration attempt, we'll use the Launch Services API
            // However, .NET doesn't have direct bindings for this, so we'll use a workaround
            
            // Try to register using duti if available
            try
            {
                // First check if duti is installed
                var checkDuti = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "duti",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                var dutiCheck = Process.Start(checkDuti);
                dutiCheck?.WaitForExit(1000);

                if (dutiCheck?.ExitCode == 0)
                {
                    // Use duti to set file association
                    var bundleId = GetMacOSBundleIdentifier();
                    if (!string.IsNullOrEmpty(bundleId))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "duti",
                            Arguments = $"-s {bundleId} {FileExtension} all",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(psi)?.WaitForExit(5000);
                    }
                }
            }
            catch { /* duti not available, skip */ }

            Log.Info("macOS file association: Info.plist configuration should be used for proper app bundle association");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to register macOS file association: {ex}");
        }
    }

    private static string? GetMacOSBundleIdentifier()
    {
        try
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(executablePath))
                return null;

            // Navigate up to find Info.plist
            var dir = Path.GetDirectoryName(executablePath);
            while (dir != null)
            {
                var infoPlistPath = Path.Combine(dir, "Contents", "Info.plist");
                if (File.Exists(infoPlistPath))
                {
                    // Try to extract CFBundleIdentifier from Info.plist
                    var content = File.ReadAllText(infoPlistPath);
                    var bundleIdStart = content.IndexOf("<key>CFBundleIdentifier</key>");
                    if (bundleIdStart >= 0)
                    {
                        var stringStart = content.IndexOf("<string>", bundleIdStart);
                        var stringEnd = content.IndexOf("</string>", stringStart);
                        if (stringStart >= 0 && stringEnd >= 0)
                        {
                            return content.Substring(stringStart + 8, stringEnd - stringStart - 8).Trim();
                        }
                    }
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }
        }
        catch { }
        return null;
    }

    // Windows API for notifying shell about file association changes
    [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
