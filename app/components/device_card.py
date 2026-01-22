# coding:utf-8
from PyQt6.QtCore import Qt, pyqtSignal, QSize
from PyQt6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QLabel
from qfluentwidgets import CardWidget, PrimaryPushButton, StrongBodyLabel, CaptionLabel, PushButton, ToolButton, FluentIcon as FIF
from app.common.scrcpy_manager import ScrcpyManager

class DeviceCard(CardWidget):
    """ Device Card Widget """
    
    mirrorSignal = pyqtSignal(str)
    wirelessSignal = pyqtSignal(str)
    
    def __init__(self, serial, model="Android Device", parent=None):
        super().__init__(parent)
        self.serial = serial
        self.model = model
        
        self.setFixedSize(360, 140) # Increased height for toolbar
        
        self.vBoxLayout = QVBoxLayout(self)
        self.vBoxLayout.setContentsMargins(20, 16, 20, 16)
        self.vBoxLayout.setSpacing(12)
        
        # Header (Model + Serial)
        self.headerLayout = QVBoxLayout()
        self.headerLayout.setSpacing(2)
        
        self.modelLabel = StrongBodyLabel(self.model, self)
        self.serialLabel = CaptionLabel(self.serial, self)
        self.serialLabel.setTextColor("#666666", "#999999")
        
        self.headerLayout.addWidget(self.modelLabel)
        self.headerLayout.addWidget(self.serialLabel)
        
        self.vBoxLayout.addLayout(self.headerLayout)
        
        # Action Buttons
        self.buttonLayout = QHBoxLayout()
        self.buttonLayout.setSpacing(10)
        
        self.mirrorButton = PrimaryPushButton(self.tr("Start Mirroring"), self)
        self.mirrorButton.setFixedWidth(120)
        self.mirrorButton.clicked.connect(self.__onMirrorClicked)
        
        self.wirelessButton = PushButton(self.tr("Wireless"), self)
        self.wirelessButton.setFixedWidth(100)
        self.wirelessButton.setToolTip(self.tr("Switch to Wireless Mode"))
        self.wirelessButton.clicked.connect(self.__onWirelessClicked)
        
        self.buttonLayout.addWidget(self.mirrorButton)
        self.buttonLayout.addWidget(self.wirelessButton)
        self.buttonLayout.addStretch(1)
        
        self.vBoxLayout.addLayout(self.buttonLayout)

        # ADB Control Toolbar
        self.toolbarLayout = QHBoxLayout()
        self.toolbarLayout.setSpacing(8)
        
        self.btnHome = ToolButton(FIF.HOME, self)
        self.btnHome.setToolTip("Home")
        self.btnHome.clicked.connect(lambda: ScrcpyManager.send_keyevent(self.serial, "KEYCODE_HOME"))
        
        self.btnRecents = ToolButton(FIF.APPLICATION, self)
        self.btnRecents.setToolTip("Recents")
        self.btnRecents.clicked.connect(lambda: ScrcpyManager.send_keyevent(self.serial, "KEYCODE_APP_SWITCH"))
        
        self.btnBack = ToolButton(FIF.RETURN, self)
        self.btnBack.setToolTip("Back")
        self.btnBack.clicked.connect(lambda: ScrcpyManager.send_keyevent(self.serial, "KEYCODE_BACK"))
        
        self.btnMute = ToolButton(FIF.MUTE, self)
        self.btnMute.setToolTip("Volume Mute")
        self.btnMute.clicked.connect(lambda: ScrcpyManager.send_keyevent(self.serial, "KEYCODE_VOLUME_MUTE"))
        
        self.btnPower = ToolButton(FIF.POWER_BUTTON, self)
        self.btnPower.setToolTip("Power")
        self.btnPower.clicked.connect(lambda: ScrcpyManager.send_keyevent(self.serial, "KEYCODE_POWER"))
        
        self.toolbarLayout.addWidget(self.btnHome)
        self.toolbarLayout.addWidget(self.btnRecents)
        self.toolbarLayout.addWidget(self.btnBack)
        self.toolbarLayout.addWidget(self.btnMute)
        self.toolbarLayout.addWidget(self.btnPower)
        self.toolbarLayout.addStretch(1)
        
        self.vBoxLayout.addLayout(self.toolbarLayout)

    def __onMirrorClicked(self):
        self.mirrorSignal.emit(self.serial)

    def __onWirelessClicked(self):
        self.wirelessSignal.emit(self.serial)
