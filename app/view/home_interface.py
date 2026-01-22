# coding:utf-8
from PyQt6.QtCore import Qt
from PyQt6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout

from qfluentwidgets import ScrollArea, TitleLabel, PushButton, FluentIcon as FIF, InfoBar, LineEdit, Dialog
from app.common.scrcpy_manager import ScrcpyManager
from app.components.device_card import DeviceCard

class HomeInterface(ScrollArea):
    """ Home Interface """

    def __init__(self, parent=None):
        super().__init__(parent)
        self.view = QWidget(self)
        self.vBoxLayout = QVBoxLayout(self.view)
        
        self.titleLabel = TitleLabel(self.tr("Connected Devices"), self.view)
        
        self.headerLayout = QHBoxLayout()
        self.connectIpEdit = LineEdit(self.view)
        self.connectIpEdit.setPlaceholderText(self.tr("Device IP:Port (e.g. 192.168.1.100:5555)"))
        self.connectIpEdit.setFixedWidth(250)
        
        self.connectButton = PushButton(self.tr("Connect"), self.view, FIF.WIFI)
        self.refreshButton = PushButton(self.tr("Refresh"), self.view, FIF.SYNC)
        
        self.deviceLayout = QVBoxLayout()
        
        self.__initWidget()
        self.__initLayout()
        
        self.refreshButton.clicked.connect(self.refreshDevices)
        self.connectButton.clicked.connect(self.connectWireless)
        
        # Initial refresh
        self.refreshDevices()

    def __initWidget(self):
        self.view.setObjectName('view')
        self.setObjectName('homeInterface')
        
        # Make background transparent to show Mica effect or parent background
        self.view.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.viewport().setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setStyleSheet("HomeInterface, #view { background-color: transparent; }")
        
        self.setWidget(self.view)
        self.setWidgetResizable(True)
        self.setHorizontalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAlwaysOff)

    def __initLayout(self):
        self.vBoxLayout.setContentsMargins(36, 40, 36, 36)
        self.vBoxLayout.setSpacing(20)
        
        # Header
        hBoxLayout = QHBoxLayout()
        hBoxLayout.addWidget(self.titleLabel)
        hBoxLayout.addStretch(1)
        hBoxLayout.addWidget(self.connectIpEdit)
        hBoxLayout.addWidget(self.connectButton)
        hBoxLayout.addSpacing(10)
        hBoxLayout.addWidget(self.refreshButton)
        self.vBoxLayout.addLayout(hBoxLayout)
        
        # Device List
        self.deviceLayout.setSpacing(10)
        self.deviceLayout.setAlignment(Qt.AlignmentFlag.AlignTop)
        self.vBoxLayout.addLayout(self.deviceLayout)
        self.vBoxLayout.addStretch(1)

    def refreshDevices(self):
        # Clear existing
        while self.deviceLayout.count():
            item = self.deviceLayout.takeAt(0)
            widget = item.widget()
            if widget:
                widget.deleteLater()
                
        devices = ScrcpyManager.get_devices()
        
        if not devices:
            # Show empty state? For now just nothing
            InfoBar.warning(
                title=self.tr("No Devices Found"),
                content=self.tr("Please connect your Android device via USB and enable USB Debugging."),
                parent=self
            )
            return

        for device_info in devices:
            serial = device_info["serial"]
            model = device_info["model"]
            card = DeviceCard(serial, model=model, parent=self.view)
            card.mirrorSignal.connect(self.startMirroring)
            card.wirelessSignal.connect(self.switchToWireless)
            self.deviceLayout.addWidget(card)
            
        InfoBar.success(
            title=self.tr("Devices Refreshed"),
            content=self.tr("Found {} device(s).").format(len(devices)),
            parent=self,
            duration=2000
        )

    def startMirroring(self, serial):
        ScrcpyManager.start_mirroring(serial)

    def switchToWireless(self, serial):
        # 1. Get IP
        ip = ScrcpyManager.get_device_ip(serial)
        if not ip:
            InfoBar.error(self.tr("Error"), self.tr("Could not obtain device IP address."), parent=self)
            return
            
        # 2. Enable TCP/IP
        if not ScrcpyManager.tcpip_mode(serial):
            InfoBar.error(self.tr("Error"), self.tr("Failed to enable TCP/IP mode."), parent=self)
            return

        # 3. Connect
        if ScrcpyManager.connect_wireless(ip):
            InfoBar.success(self.tr("Success"), self.tr("Wireless connection established. You can now unplug USB."), parent=self, duration=3000)
            self.refreshDevices()
        else:
            InfoBar.error(self.tr("Error"), self.tr("Failed to connect wirelessly."), parent=self)

    def connectWireless(self):
        addr = self.connectIpEdit.text().strip()
        if not addr:
            InfoBar.warning(self.tr("Warning"), self.tr("Please enter an IP address."), parent=self)
            return
            
        parts = addr.split(':')
        ip = parts[0]
        port = 5555
        if len(parts) > 1:
            try:
                port = int(parts[1])
            except ValueError:
                pass
                
        if ScrcpyManager.connect_wireless(ip, port):
            InfoBar.success(self.tr("Success"), self.tr("Connected to device."), parent=self)
            self.refreshDevices()
        else:
            InfoBar.error(self.tr("Error"), self.tr("Failed to connect to device."), parent=self)
