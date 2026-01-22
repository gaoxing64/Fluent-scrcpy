# coding:utf-8
from PyQt6.QtCore import Qt, QUrl
from PyQt6.QtGui import QIcon, QDesktopServices
from PyQt6.QtWidgets import QApplication, QFrame, QHBoxLayout

from qfluentwidgets import (NavigationItemPosition, FluentWindow, SubtitleLabel, setFont, 
                            SplashScreen)
from qfluentwidgets import FluentIcon as FIF

from app.common.config import cfg
from app.view.settings_interface import SettingsInterface
from app.view.home_interface import HomeInterface
from app.view.config_interface import ConfigInterface

from app.common.scrcpy_manager import ScrcpyManager

class MainWindow(FluentWindow):

    def __init__(self):
        super().__init__()
        self.initWindow()

        # Create sub interfaces
        self.homeInterface = HomeInterface(self)
        self.configInterface = ConfigInterface(self)
        self.settingsInterface = SettingsInterface(self)

        self.initInterface()
        self.initNavigation()
        self.initRuntimeSignals()

    def initRuntimeSignals(self):
        """ Connect config signals to runtime controllers """
        # We need to iterate over all active devices to apply changes.
        # But signals don't pass serials. So we apply to ALL active devices.
        
        def apply_to_all(func, value):
            # ScrcpyManager needs a way to list active serials. 
            # We can expose keys of _active_processes
            for serial in ScrcpyManager._active_processes.keys():
                func(serial, value)

        cfg.stayAwake.valueChanged.connect(lambda v: apply_to_all(ScrcpyManager.update_stay_awake, v))
        cfg.alwaysOnTop.valueChanged.connect(lambda v: apply_to_all(ScrcpyManager.update_always_on_top, v))
        cfg.fullscreen.valueChanged.connect(lambda v: apply_to_all(ScrcpyManager.update_fullscreen, v))
        cfg.fixAspectRatio.valueChanged.connect(lambda v: apply_to_all(ScrcpyManager.update_aspect_ratio_lock, v))

    def initNavigation(self):
        self.addSubInterface(self.homeInterface, FIF.HOME, self.tr("Home"))
        self.addSubInterface(self.configInterface, FIF.EDIT, self.tr("Configuration"))
        self.addSubInterface(self.settingsInterface, FIF.SETTING, self.tr("App Settings"), NavigationItemPosition.BOTTOM)
        
        # Enable Acrylic effect for navigation interface
        self.navigationInterface.setAcrylicEnabled(True)
        # Adjust navigation expand width to be more compact
        self.navigationInterface.setExpandWidth(200)

    def initWindow(self):
        self.resize(900, 700)
        self.setWindowIcon(QIcon('app/resource/logo.png'))
        self.setWindowTitle('Scrcpy GUI')

        desktop = QApplication.screens()[0].availableGeometry()
        w, h = desktop.width(), desktop.height()
        self.move(w//2 - self.width()//2, h//2 - self.height()//2)

    def initInterface(self):
        # Placeholder for Home Interface
        # layout = QHBoxLayout(self.homeInterface)
        # label = SubtitleLabel("Home Interface Placeholder", self.homeInterface)
        # layout.addWidget(label, 0, Qt.AlignmentFlag.AlignCenter)
        self.homeInterface.setObjectName('homeInterface')

        self.configInterface.setObjectName('configInterface')

        # Placeholder for Settings Interface
        # layout = QHBoxLayout(self.settingsInterface)
        # label = SubtitleLabel("Settings Interface Placeholder", self.settingsInterface)
        # layout.addWidget(label, 0, Qt.AlignmentFlag.AlignCenter)
        self.settingsInterface.setObjectName('settingsInterface')
