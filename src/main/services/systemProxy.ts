import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

export class WindowsSystemProxyManager {
  private originalProxyEnable: number | null = null;
  private originalProxyServer: string | null = null;
  private isApplied = false;

  async getProxyStatus(): Promise<{ enabled: boolean; server: string }> {
    try {
      const { stdout } = await execAsync(
        'reg query "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyEnable'
      );
      const isEnabled = stdout.includes('0x1');

      let server = '';
      try {
        const serverOut = await execAsync(
          'reg query "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyServer'
        );
        const match = serverOut.stdout.match(/ProxyServer\s+REG_SZ\s+(.*)/i);
        if (match) server = match[1].trim();
      } catch {}

      return { enabled: isEnabled, server };
    } catch {
      return { enabled: false, server: '' };
    }
  }

  async enable(httpPort = 8080, socksPort = 1080): Promise<boolean> {
    try {
      // 1. Backup original configuration if not already backed up
      if (this.originalProxyEnable === null) {
        const current = await this.getProxyStatus();
        this.originalProxyEnable = current.enabled ? 1 : 0;
        this.originalProxyServer = current.server;
      }

      // 2. Configure ProxyServer string for both HTTP and SOCKS
      const proxyString = `http=127.0.0.1:${httpPort};https=127.0.0.1:${httpPort};socks=127.0.0.1:${socksPort}`;

      await execAsync(
        `reg add "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyEnable /t REG_DWORD /d 1 /f`
      );
      await execAsync(
        `reg add "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyServer /t REG_SZ /d "${proxyString}" /f`
      );

      // 3. Flush WinINet settings cache
      await this.refreshWinINet();
      this.isApplied = true;
      return true;
    } catch (err) {
      console.error('Failed to enable Windows system proxy:', err);
      return false;
    }
  }

  async disable(): Promise<boolean> {
    try {
      if (this.originalProxyEnable !== null && this.originalProxyEnable === 1 && this.originalProxyServer) {
        // Restore previous proxy configuration
        await execAsync(
          `reg add "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyEnable /t REG_DWORD /d 1 /f`
        );
        await execAsync(
          `reg add "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyServer /t REG_SZ /d "${this.originalProxyServer}" /f`
        );
      } else {
        // Turn proxy off completely
        await execAsync(
          `reg add "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings" /v ProxyEnable /t REG_DWORD /d 0 /f`
        );
      }

      await this.refreshWinINet();
      this.isApplied = false;
      return true;
    } catch (err) {
      console.error('Failed to disable Windows system proxy:', err);
      return false;
    }
  }

  private async refreshWinINet(): Promise<void> {
    try {
      // Refresh WinINet settings in powershell using InternetSetOption
      const refreshScript = `
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class WinINet {
  [DllImport("wininet.dll", SetLastError = true)]
  public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
}
"@
[WinINet]::InternetSetOption([IntPtr]::Zero, 39, [IntPtr]::Zero, 0) | Out-Null
[WinINet]::InternetSetOption([IntPtr]::Zero, 37, [IntPtr]::Zero, 0) | Out-Null
`;
      await execAsync(`powershell -NoProfile -Command "${refreshScript.replace(/\r?\n/g, ' ')}"`, { timeout: 3000 });
    } catch {
      // Fallback: wininet will update when applications query registry
    }
  }

  isCurrentlyApplied(): boolean {
    return this.isApplied;
  }
}
