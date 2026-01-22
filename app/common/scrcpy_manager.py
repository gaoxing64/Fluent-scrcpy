# coding:utf-8
import subprocess
import threading
import time
from adbutils import adb
from app.common.config import cfg
from .window_controller import WindowController

class ScrcpyManager:
    """ Scrcpy Manager for handling device connection and mirroring """

    _active_processes = {} # serial -> subprocess.Popen
    _active_models = {} # serial -> model name (for window finding)

    @staticmethod
    def get_devices():
        """ Get list of connected devices with details """
        try:
            devices = []
            for d in adb.device_list():
                model = d.prop.model if d.prop.model else "Android Device"
                devices.append({"serial": d.serial, "model": model})
            return devices
        except Exception as e:
            print(f"Error getting devices: {e}")
            return []

    @staticmethod
    def get_device_ip(serial):
        """ Get device IP address """
        try:
            # Try to get IP from wlan0 interface
            result = adb.device(serial=serial).shell("ip route")
            # Typical output: "192.168.1.0/24 dev wlan0 proto kernel scope link src 192.168.1.123"
            # We want to find the line with wlan0 and extract src IP
            for line in result.splitlines():
                if "wlan0" in line and "src" in line:
                    parts = line.split()
                    if "src" in parts:
                        idx = parts.index("src")
                        if idx + 1 < len(parts):
                            return parts[idx + 1]
            
            # Fallback: ip addr show wlan0
            result = adb.device(serial=serial).shell("ip addr show wlan0")
            # inet 192.168.1.123/24 ...
            for line in result.splitlines():
                line = line.strip()
                if line.startswith("inet "):
                    return line.split()[1].split('/')[0]
                    
            return None
        except Exception as e:
            print(f"Error getting device IP: {e}")
            return None

    @staticmethod
    def tcpip_mode(serial, port=5555):
        """ Switch device to TCP/IP mode """
        try:
            adb.device(serial=serial).tcpip(port)
            return True
        except Exception as e:
            print(f"Error switching to tcpip mode: {e}")
            return False

    @staticmethod
    def connect_wireless(ip, port=5555):
        """ Connect to device via TCP/IP """
        try:
            # adbutils doesn't seem to have a direct connect method on the client?
            # It does: adb.connect(addr)
            output = adb.connect(f"{ip}:{port}")
            # Output is usually string "connected to ..." or "already connected to ..."
            return "connected" in output.lower()
        except Exception as e:
            print(f"Error connecting wireless: {e}")
            return False

    @staticmethod
    def build_command(serial):
        """ Build scrcpy command based on configuration """
        cmd = ["scrcpy", "-s", serial]
        
        # Video
        max_size = cfg.get(cfg.videoMaxSize)
        if max_size != 0:
            cmd.extend(["--max-size", str(max_size)])
            
        bitrate = cfg.get(cfg.videoBitrate)
        cmd.extend(["--video-bit-rate", f"{bitrate}M"])
        
        fps = cfg.get(cfg.videoFps)
        if fps != 0:
            cmd.extend(["--max-fps", str(fps)])
            
        codec = cfg.get(cfg.videoCodec)
        cmd.extend(["--video-codec", codec])
        
        # Aspect Ratio Lock (Crop)
        if cfg.get(cfg.fixAspectRatio):
            try:
                # Get device resolution
                output = adb.device(serial=serial).shell("wm size")
                # Output: "Physical size: 1080x2400"
                if "Physical size:" in output:
                    res_str = output.split("Physical size:")[1].strip()
                    width, height = map(int, res_str.split('x'))
                    
                    # Calculate current aspect ratio
                    # We want to maintain this ratio. scrcpy doesn't have a direct "lock ratio" flag
                    # for window resizing behavior that is independent of content.
                    # However, --crop can be used to enforce a specific ratio if needed, 
                    # but user asked for "lock ratio" when resizing window.
                    # scrcpy actually maintains aspect ratio by default unless you press Alt+w or similar.
                    # BUT, if the user wants to FORCE it to stay at device ratio even if they try to stretch it weirdly?
                    # scrcpy default behavior IS to keep aspect ratio and add black bars if window size doesn't match.
                    # If the user wants the window ITSELF to lock ratio, that's a window manager function (SDL).
                    # scrcpy doesn't strictly support "window resize lock" via CLI args directly other than initial size.
                    
                    # Wait, the user said: "if res is 1920x1080 (16:9), maintain 16:9 when resizing".
                    # This implies they don't want black bars. They want the window frame to lock ratio.
                    # Standard scrcpy (SDL) window is resizable and keeps content ratio (adding black bars).
                    # There is no flag in scrcpy to lock the OS window aspect ratio.
                    # BUT, we can try to pass specific size or maybe this feature is not fully implementable via CLI only.
                    
                    # Actually, re-reading the user prompt: "not GUI ratio, but scrcpy software core display ratio".
                    # If they mean the scrcpy window itself:
                    # We can't easily control the external scrcpy window's resize behavior from here once launched.
                    # UNLESS we embed scrcpy. But we are launching it as subprocess.
                    
                    # However, if we interpret "Lock Ratio" as "Don't allow black bars / Force content to fill",
                    # scrcpy always maintains device aspect ratio.
                    
                    # Let's assume the user might be experiencing something else or wants to ensure
                    # the device resolution is respected.
                    # Maybe they want "--window-width" and "--window-height" ? No.
                    
                    # The user said: "calculate ratio... lock this ratio".
                    # If I use `scrcpy --crop ...`, I can force a ratio.
                    # But if the device is already 16:9, I don't need to crop.
                    
                    pass # Placeholder for now as scrcpy handles ratio by default.
                    
            except Exception as e:
                print(f"Error getting resolution for ratio lock: {e}")

        # Control
        if cfg.get(cfg.turnScreenOff):
            cmd.append("--turn-screen-off")
            
        if cfg.get(cfg.stayAwake):
            cmd.append("--stay-awake")

        # Window
        if cfg.get(cfg.alwaysOnTop):
            cmd.append("--always-on-top")
            
        if cfg.get(cfg.fullscreen):
            cmd.append("--fullscreen")

        return cmd

    @staticmethod
    def start_mirroring(serial):
        """ Start scrcpy for the given serial """
        # Fetch model name first for window tracking
        try:
            model = adb.device(serial=serial).prop.model
            if not model: model = serial
            ScrcpyManager._active_models[serial] = model
        except:
            ScrcpyManager._active_models[serial] = serial

        cmd = ScrcpyManager.build_command(serial)
        print(f"Executing: {' '.join(cmd)}")
        
        try:
            # Run in background
            proc = subprocess.Popen(cmd, creationflags=subprocess.CREATE_NO_WINDOW)
            ScrcpyManager._active_processes[serial] = proc
            
            # Start Aspect Ratio Enforcer if enabled
            if cfg.get(cfg.fixAspectRatio):
                threading.Thread(target=ScrcpyManager._aspect_ratio_supervisor, args=(serial,), daemon=True).start()
                
        except Exception as e:
            print(f"Error starting scrcpy: {e}")

    @staticmethod
    def stop_mirroring(serial):
        """ Stop scrcpy for the given serial """
        if serial in ScrcpyManager._active_processes:
            try:
                ScrcpyManager._active_processes[serial].terminate()
                del ScrcpyManager._active_processes[serial]
            except:
                pass

    @staticmethod
    def _get_window_handle(serial):
        """ Helper to get HWND for a serial """
        # Find window by Process ID
        if serial in ScrcpyManager._active_processes:
            proc = ScrcpyManager._active_processes[serial]
            hwnd = WindowController.find_scrcpy_window_by_pid(proc.pid)
            if not hwnd:
                print(f"Warning: Could not find window for serial {serial} (PID: {proc.pid})")
            return hwnd
        return None

    # --- Runtime Control Methods ---

    @staticmethod
    def send_keyevent(serial, keycode):
        """ Send ADB keyevent """
        try:
            adb.device(serial=serial).shell(f"input keyevent {keycode}")
        except Exception as e:
            print(f"Error sending keyevent: {e}")

    @staticmethod
    def update_stay_awake(serial, enabled):
        """ Update stay_awake via ADB """
        try:
            # true = on, false = off (restore timeout)
            val = "true" if enabled else "false"
            adb.device(serial=serial).shell(f"svc power stayon {val}")
        except Exception as e:
            print(f"Error updating stay_awake: {e}")

    @staticmethod
    def update_always_on_top(serial, enabled):
        """ Update window always on top """
        hwnd = ScrcpyManager._get_window_handle(serial)
        if hwnd:
            WindowController.set_always_on_top(hwnd, enabled)

    @staticmethod
    def update_fullscreen(serial, enabled):
        """ Update fullscreen mode manually """
        hwnd = ScrcpyManager._get_window_handle(serial)
        if hwnd:
            WindowController.set_fake_fullscreen(hwnd, enabled)

    @staticmethod
    def update_aspect_ratio_lock(serial, enabled):
        """ Trigger aspect ratio check/resize """
        if enabled:
            # Start supervisor if not running (or just trigger one-time snap)
            threading.Thread(target=ScrcpyManager._aspect_ratio_supervisor, args=(serial,), daemon=True).start()

    @staticmethod
    def _aspect_ratio_supervisor(serial):
        """ Background thread to enforce aspect ratio """
        # Give scrcpy some time to launch window
        time.sleep(1)
        
        # Get target ratio from device
        try:
            output = adb.device(serial=serial).shell("wm size")
            if "Physical size:" not in output: return
            res_str = output.split("Physical size:")[1].strip()
            w_dev, h_dev = map(int, res_str.split('x'))
            target_ratio = w_dev / h_dev
        except:
            return

        # Loop while process is alive and config is enabled
        while serial in ScrcpyManager._active_processes and cfg.get(cfg.fixAspectRatio):
            proc = ScrcpyManager._active_processes[serial]
            if proc.poll() is not None: break
            
            hwnd = ScrcpyManager._get_window_handle(serial)
            if hwnd:
                rect = WindowController.get_window_rect(hwnd)
                if rect:
                    x, y, w, h = rect
                    current_ratio = w / h
                    # Check deviation (allow small error)
                    if abs(current_ratio - target_ratio) > 0.02:
                        # Enforce width, adjust height? Or preserve larger dim?
                        # Let's preserve width
                        new_h = int(w / target_ratio)
                        WindowController.set_window_size(hwnd, w, new_h)
            
            time.sleep(0.5) # Check every 500ms
