using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;

namespace ClassicUO.Utility
{
    internal static class ClientUpdateApplier
    {
        internal static bool TryStartPostExitUpdate(string zipPath, out string error)
        {
            error = null;
            try
            {
                string installDir = Path.GetFullPath(CUOEnviroment.ExecutablePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    error = "Could not resolve the running executable path.";
                    return false;
                }

                exePath = Path.GetFullPath(exePath);
                int pid = Environment.ProcessId;
                string scriptPath = Path.Combine(Path.GetTempPath(), "dust765_apply_" + Guid.NewGuid().ToString("N"));

                if (PlatformHelper.IsWindows)
                {
                    scriptPath += ".ps1";
                    string body = BuildWindowsScript();
                    File.WriteAllText(scriptPath, body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    return LaunchWindows(scriptPath, zipPath, installDir, exePath, pid, out error);
                }

                if (PlatformHelper.IsLinux || PlatformHelper.IsOSX)
                {
                    scriptPath += ".sh";
                    string body = BuildUnixScript();
                    File.WriteAllText(scriptPath, body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    return LaunchUnix(scriptPath, zipPath, installDir, exePath, pid, out error);
                }

                error = "Automatic updates are not supported on this platform.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string BuildWindowsScript()
        {
            return @"$ErrorActionPreference = 'Stop'
Start-Sleep -Seconds 2
$clientPid = [int]$env:DUST765_CLIENT_PID
$zip = $env:DUST765_ZIP
$dir = $env:DUST765_DIR
$exe = $env:DUST765_EXE
$self = $env:DUST765_SCRIPT
try { Stop-Process -Id $clientPid -Force -ErrorAction SilentlyContinue } catch {}
$deadline = (Get-Date).AddMinutes(3)
while ($null -ne (Get-Process -Id $clientPid -ErrorAction SilentlyContinue)) {
  if ((Get-Date) -gt $deadline) { break }
  Start-Sleep -Milliseconds 400
}
Expand-Archive -LiteralPath $zip -DestinationPath $dir -Force
$dirs = @(Get-ChildItem -LiteralPath $dir -Directory -ErrorAction SilentlyContinue)
$files = @(Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue)
if ($dirs.Count -eq 1 -and $files.Count -eq 0) {
  $inner = $dirs[0].FullName
  Get-ChildItem -LiteralPath $inner -Force | ForEach-Object {
    $target = Join-Path $dir $_.Name
    if ($_.PSIsContainer) {
      Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force
    } else {
      Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    }
  }
  Remove-Item -LiteralPath $inner -Recurse -Force
}
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
Start-Process -LiteralPath $exe -WorkingDirectory $dir
Remove-Item -LiteralPath $self -Force -ErrorAction SilentlyContinue
";
        }

        private static string BuildUnixScript()
        {
            return @"#!/usr/bin/env bash
set -euo pipefail
sleep 2
kill -TERM ""${DUST765_CLIENT_PID}"" 2>/dev/null || true
i=0
while [ $i -lt 90 ]; do
  if ! kill -0 ""${DUST765_CLIENT_PID}"" 2>/dev/null; then break; fi
  sleep 1
  i=$((i+1))
done
unzip -o -qq ""${DUST765_ZIP}"" -d ""${DUST765_DIR}""
nd=$(find ""${DUST765_DIR}"" -mindepth 1 -maxdepth 1 -type d 2>/dev/null | wc -l | tr -d ' ')
nf=$(find ""${DUST765_DIR}"" -mindepth 1 -maxdepth 1 -type f 2>/dev/null | wc -l | tr -d ' ')
if [ ""$nd"" -eq 1 ] && [ ""$nf"" -eq 0 ]; then
  inner=$(find ""${DUST765_DIR}"" -mindepth 1 -maxdepth 1 -type d 2>/dev/null | head -1)
  shopt -s dotglob nullglob
  for p in ""$inner""/*; do
    base=$(basename ""$p"")
    rm -rf ""${DUST765_DIR}/$base""
    mv ""$p"" ""${DUST765_DIR}/""
  done
  rmdir ""$inner"" 2>/dev/null || true
fi
chmod +x ""${DUST765_EXE}"" 2>/dev/null || true
rm -f ""${DUST765_ZIP}""
nohup ""${DUST765_EXE}"" >/dev/null 2>&1 &
disown 2>/dev/null || true
rm -f ""${DUST765_SCRIPT}""
";
        }

        private static bool LaunchWindows(string scriptPath, string zipPath, string installDir, string exePath, int pid, out string error)
        {
            error = null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.Environment["DUST765_CLIENT_PID"] = pid.ToString();
                psi.Environment["DUST765_ZIP"] = zipPath;
                psi.Environment["DUST765_DIR"] = installDir;
                psi.Environment["DUST765_EXE"] = exePath;
                psi.Environment["DUST765_SCRIPT"] = scriptPath;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                error = ex.Message;
                return false;
            }
        }

        private static bool LaunchUnix(string scriptPath, string zipPath, string installDir, string exePath, int pid, out string error)
        {
            error = null;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.Environment["DUST765_CLIENT_PID"] = pid.ToString();
                psi.Environment["DUST765_ZIP"] = zipPath;
                psi.Environment["DUST765_DIR"] = installDir;
                psi.Environment["DUST765_EXE"] = exePath;
                psi.Environment["DUST765_SCRIPT"] = scriptPath;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                error = ex.Message;
                return false;
            }
        }
    }
}
