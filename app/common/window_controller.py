# coding:utf-8
import ctypes
from ctypes import wintypes
import time
import threading

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

SWP_NOMOVE = 0x0002
SWP_NOSIZE = 0x0001
SWP_NOZORDER = 0x0004
SWP_SHOWWINDOW = 0x0040
SWP_FRAMECHANGED = 0x0020
HWND_TOPMOST = -1
HWND_NOTOPMOST = -2

GWL_STYLE = -16
WS_CAPTION = 0x00C00000
WS_THICKFRAME = 0x00040000
WS_POPUP = 0x80000000

GWL_EXSTYLE = -20
WS_EX_TOPMOST = 0x00000008

class MONITORINFO(ctypes.Structure):
    _fields_ = [
        ("cbSize", wintypes.DWORD),
        ("rcMonitor", wintypes.RECT),
        ("rcWork", wintypes.RECT),
        ("dwFlags", wintypes.DWORD),
    ]

wintypes.MONITORINFO = MONITORINFO

class WindowController:
    """ Helper to control external windows via Win32 API """
    
    @staticmethod
    def find_scrcpy_window_by_pid(pid):
        """ Find window handle by Process ID """
        target_hwnd = None
        
        def enum_handler(hwnd, ctx):
            nonlocal target_hwnd
            if not user32.IsWindowVisible(hwnd):
                return True
            
            # Check PID
            window_pid = ctypes.c_ulong()
            user32.GetWindowThreadProcessId(hwnd, ctypes.byref(window_pid))
            
            if window_pid.value == pid:
                # Also check window class/title to avoid hidden helper windows
                # Scrcpy main window usually has title = device model or serial
                # But PID match is usually enough if it's the main visible window
                target_hwnd = hwnd
                return False # Found
            return True

        WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, ctypes.POINTER(ctypes.c_int))
        user32.EnumWindows(WNDENUMPROC(enum_handler), 0)
        return target_hwnd

    @staticmethod
    def set_always_on_top(hwnd, enable):
        if not hwnd: return
        
        # Method 1: SetWindowPos
        flags = SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
        hwnd_insert_after = HWND_TOPMOST if enable else HWND_NOTOPMOST
        user32.SetWindowPos(hwnd, hwnd_insert_after, 0, 0, 0, 0, flags)
        
        # Method 2: Force GWL_EXSTYLE if SetWindowPos fails to persist (some apps override it)
        ex_style = user32.GetWindowLongW(hwnd, GWL_EXSTYLE)
        if enable:
            if not (ex_style & WS_EX_TOPMOST):
                user32.SetWindowLongW(hwnd, GWL_EXSTYLE, ex_style | WS_EX_TOPMOST)
                # Trigger update
                user32.SetWindowPos(hwnd, hwnd_insert_after, 0, 0, 0, 0, flags | SWP_FRAMECHANGED)
        else:
            if ex_style & WS_EX_TOPMOST:
                user32.SetWindowLongW(hwnd, GWL_EXSTYLE, ex_style & ~WS_EX_TOPMOST)
                # Trigger update
                user32.SetWindowPos(hwnd, hwnd_insert_after, 0, 0, 0, 0, flags | SWP_FRAMECHANGED)

    @staticmethod
    def set_borderless(hwnd, enable):
        if not hwnd: return
        style = user32.GetWindowLongW(hwnd, GWL_STYLE)
        
        if enable:
            # Remove caption and thick frame
            style &= ~(WS_CAPTION | WS_THICKFRAME)
            # Add popup to make it strictly borderless? Or just remove decorations.
        else:
            # Restore
            style |= (WS_CAPTION | WS_THICKFRAME)
            
        user32.SetWindowLongW(hwnd, GWL_STYLE, style)
        
        # Trigger frame redraw
        user32.SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | 0x0020) # SWP_FRAMECHANGED

    @staticmethod
    def set_fake_fullscreen(hwnd, enable):
        """ Toggle borderless fullscreen mode manually """
        if not hwnd: return
        
        style = user32.GetWindowLongW(hwnd, GWL_STYLE)
        
        if enable:
            # Store original style/rect if needed (omitted for simplicity, assume toggle off restores defaults or we recalc)
            # Remove decorations
            style &= ~(WS_CAPTION | WS_THICKFRAME)
            user32.SetWindowLongW(hwnd, GWL_STYLE, style)
            
            # Maximize to screen
            hmonitor = user32.MonitorFromWindow(hwnd, 2) # MONITOR_DEFAULTTONEAREST
            info = wintypes.MONITORINFO()
            info.cbSize = ctypes.sizeof(wintypes.MONITORINFO)
            user32.GetMonitorInfoW(hmonitor, ctypes.byref(info))
            
            rect = info.rcMonitor
            user32.SetWindowPos(hwnd, 0, rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top, SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW)
        else:
            # Restore windowed mode
            style |= (WS_CAPTION | WS_THICKFRAME)
            user32.SetWindowLongW(hwnd, GWL_STYLE, style)
            # We don't restore exact size here, user can resize. Just make it valid.
            user32.SetWindowPos(hwnd, 0, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED)

    @staticmethod
    def set_window_size(hwnd, width, height):
        if not hwnd: return
        user32.SetWindowPos(hwnd, 0, 0, 0, width, height, SWP_NOMOVE | SWP_SHOWWINDOW)

    @staticmethod
    def get_window_rect(hwnd):
        if not hwnd: return None
        rect = wintypes.RECT()
        user32.GetWindowRect(hwnd, ctypes.byref(rect))
        return (rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top)
