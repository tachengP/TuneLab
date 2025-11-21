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
            // We'll try multiple approaches to register the file association
            
            // First, try to create/update Info.plist in the app bundle if we're in one
            var appBundlePath = FindMacOSAppBundle(executablePath);
            if (!string.IsNullOrEmpty(appBundlePath))
            {
                var infoPlistPath = Path.Combine(appBundlePath, "Contents", "Info.plist");
                CreateOrUpdateMacOSInfoPlist(infoPlistPath, executablePath);
                
                // Try to refresh Launch Services
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister",
                        Arguments = $"-f \"{appBundlePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psi)?.WaitForExit(5000);
                }
                catch { /* lsregister might not be accessible */ }
            }
            
            // Also try to register using duti if available
            try
            {
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
                    var bundleId = GetMacOSBundleIdentifier() ?? "com.tunelab.TuneLab";
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
            catch { /* duti not available, skip */ }

            Log.Info("macOS file association registered");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to register macOS file association: {ex}");
        }
    }

    private static string? FindMacOSAppBundle(string executablePath)
    {
        var dir = Path.GetDirectoryName(executablePath);
        while (dir != null)
        {
            if (dir.EndsWith(".app"))
                return dir;
            var parentDir = Path.GetDirectoryName(dir);
            if (parentDir == dir) // Reached root
                break;
            dir = parentDir;
        }
        return null;
    }

    private static void CreateOrUpdateMacOSInfoPlist(string infoPlistPath, string executablePath)
    {
        try
        {
            var contentsDir = Path.GetDirectoryName(infoPlistPath);
            var resourcesDir = Path.Combine(contentsDir!, "Resources");
            PathManager.MakeSureExist(contentsDir!);
            PathManager.MakeSureExist(resourcesDir);

            // Copy icons to Resources folder
            var fileIconPng = Path.Combine(PathManager.ExcutableFolder, "Assets", "file.png");
            var appIconPng = Path.Combine(PathManager.ExcutableFolder, "Assets", "app.png");
            
            if (File.Exists(fileIconPng))
            {
                var targetIconPath = Path.Combine(resourcesDir, "file.icns");
                // For simplicity, copy PNG (in production, should convert to .icns)
                try
                {
                    // Try to convert PNG to ICNS using sips (macOS built-in tool)
                    var tempIconSet = Path.Combine(Path.GetTempPath(), "file.iconset");
                    PathManager.MakeSureExist(tempIconSet);
                    
                    // Copy file.png to multiple sizes in iconset
                    var iconSizes = new[] { 16, 32, 128, 256, 512 };
                    foreach (var size in iconSizes)
                    {
                        var iconFile = Path.Combine(tempIconSet, $"icon_{size}x{size}.png");
                        var iconFile2x = Path.Combine(tempIconSet, $"icon_{size}x{size}@2x.png");
                        
                        // Use sips to resize
                        try
                        {
                            var sips = new ProcessStartInfo
                            {
                                FileName = "sips",
                                Arguments = $"-z {size} {size} \"{fileIconPng}\" --out \"{iconFile}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            };
                            Process.Start(sips)?.WaitForExit(2000);
                            
                            var sips2x = new ProcessStartInfo
                            {
                                FileName = "sips",
                                Arguments = $"-z {size * 2} {size * 2} \"{fileIconPng}\" --out \"{iconFile2x}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            };
                            Process.Start(sips2x)?.WaitForExit(2000);
                        }
                        catch { }
                    }
                    
                    // Convert iconset to icns
                    var iconutil = new ProcessStartInfo
                    {
                        FileName = "iconutil",
                        Arguments = $"-c icns \"{tempIconSet}\" -o \"{targetIconPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(iconutil)?.WaitForExit(5000);
                    
                    // Clean up temp iconset
                    try { Directory.Delete(tempIconSet, true); } catch { }
                }
                catch
                {
                    // Fallback: just copy the PNG file
                    File.Copy(fileIconPng, Path.Combine(resourcesDir, "file.png"), true);
                }
            }

            // Create or update Info.plist
            var bundleId = GetMacOSBundleIdentifier() ?? "com.tunelab.TuneLab";
            var executableName = Path.GetFileNameWithoutExtension(executablePath);
            
            var plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>CFBundleIdentifier</key>
    <string>{bundleId}</string>
    <key>CFBundleName</key>
    <string>TuneLab</string>
    <key>CFBundleDisplayName</key>
    <string>TuneLab</string>
    <key>CFBundleExecutable</key>
    <string>{executableName}</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleIconFile</key>
    <string>app</string>
    <key>CFBundleDocumentTypes</key>
    <array>
        <dict>
            <key>CFBundleTypeExtensions</key>
            <array>
                <string>tlp</string>
            </array>
            <key>CFBundleTypeIconFile</key>
            <string>file</string>
            <key>CFBundleTypeName</key>
            <string>{FileDescriptionEn}</string>
            <key>CFBundleTypeRole</key>
            <string>Editor</string>
            <key>LSHandlerRank</key>
            <string>Owner</string>
        </dict>
    </array>
</dict>
</plist>";

            File.WriteAllText(infoPlistPath, plistContent);
            Log.Info($"Created/Updated Info.plist at {infoPlistPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to create/update Info.plist: {ex}");
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
