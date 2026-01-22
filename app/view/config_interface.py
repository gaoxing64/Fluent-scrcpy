# coding:utf-8
from PyQt6.QtCore import Qt
from PyQt6.QtWidgets import QWidget, QLabel, QVBoxLayout

from qfluentwidgets import (ScrollArea, SettingCardGroup, SwitchSettingCard, OptionsSettingCard,
                            RangeSettingCard, ComboBoxSettingCard, TitleLabel, FluentIcon as FIF)
from app.common.config import cfg

class ConfigInterface(ScrollArea):
    """ Configuration interface """

    def __init__(self, parent=None):
        super().__init__(parent)
        self.scrollWidget = QWidget()
        self.expandLayout = QVBoxLayout(self.scrollWidget)
        
        # Apply style sheet
        self.scrollWidget.setObjectName('scrollWidget')
        self.setObjectName('configInterface')
        
        # Make background transparent to show Mica effect or parent background
        self.scrollWidget.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.viewport().setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setStyleSheet("ConfigInterface, #scrollWidget { background-color: transparent; }")
        self.settingLabel = TitleLabel(self.tr("Configuration"), self)

        # Settings Groups
        self.profileGroup = SettingCardGroup(self.tr("Profile"), self.scrollWidget)
        self.videoGroup = SettingCardGroup(self.tr("Video"), self.scrollWidget)
        self.controlGroup = SettingCardGroup(self.tr("Control"), self.scrollWidget)
        self.windowGroup = SettingCardGroup(self.tr("Window"), self.scrollWidget)

        # Profile Items
        self.profileCard = ComboBoxSettingCard(
            cfg.scrcpyProfile,
            FIF.SPEED_HIGH,
            self.tr("Performance Profile"),
            self.tr("Choose a preset configuration"),
            texts=[self.tr("Fast"), self.tr("High Quality"), self.tr("Custom")],
            parent=self.profileGroup
        )

        # Video Items
        self.maxSizeCard = ComboBoxSettingCard(
            cfg.videoMaxSize,
            FIF.ZOOM_IN,
            self.tr("Max Resolution"),
            "--max-size / -m",
            texts=[self.tr("Native"), "1920", "1600", "1280", "1024", "800"],
            parent=self.videoGroup
        )
        self.bitrateCard = RangeSettingCard(
            cfg.videoBitrate,
            FIF.SPEED_OFF,
            self.tr("Bitrate (Mbps)"),
            "--video-bit-rate",
            self.videoGroup
        )
        self.fpsCard = ComboBoxSettingCard(
            cfg.videoFps,
            FIF.MOVIE,
            self.tr("Frame Rate"),
            "--max-fps",
            texts=[self.tr("Auto"), "30", "60", "90", "120"],
            parent=self.videoGroup
        )
        self.codecCard = OptionsSettingCard(
            cfg.videoCodec,
            FIF.VIDEO,
            self.tr("Video Codec"),
            "--video-codec",
            ["h264", "h265", "av1"],
            self.videoGroup
        )

        # Control Items
        self.turnScreenOffCard = SwitchSettingCard(
            FIF.POWER_BUTTON,
            self.tr("Turn screen off"),
            "--turn-screen-off / -S",
            configItem=cfg.turnScreenOff,
            parent=self.controlGroup
        )
        self.stayAwakeCard = SwitchSettingCard(
            FIF.BRIGHTNESS,
            self.tr("Stay awake"),
            "--stay-awake / -w",
            configItem=cfg.stayAwake,
            parent=self.controlGroup
        )

        # Window Items
        self.alwaysOnTopCard = SwitchSettingCard(
            FIF.PIN,
            self.tr("Always on top"),
            "--always-on-top",
            configItem=cfg.alwaysOnTop,
            parent=self.windowGroup
        )
        self.fullscreenCard = SwitchSettingCard(
            FIF.FULL_SCREEN,
            self.tr("Fullscreen"),
            self.tr("Borderless Windowed Mode"),
            configItem=cfg.fullscreen,
            parent=self.windowGroup
        )
        self.fixAspectRatioCard = SwitchSettingCard(
            FIF.ZOOM_IN,
            self.tr("Lock Device Aspect Ratio"),
            self.tr("Lock window aspect ratio to match device resolution"),
            configItem=cfg.fixAspectRatio,
            parent=self.windowGroup
        )

        self.__initWidget()

    def __initWidget(self):
        self.resize(1000, 800)
        self.setHorizontalScrollBarPolicy(Qt.ScrollBarPolicy.ScrollBarAlwaysOff)
        self.setViewportMargins(0, 80, 0, 20)
        self.setWidget(self.scrollWidget)
        self.setWidgetResizable(True)

        # Initialize layout
        self.__initLayout()
        
        # Connect signals
        cfg.scrcpyProfile.valueChanged.connect(self.onProfileChanged)
        self.onProfileChanged(cfg.scrcpyProfile.value)

    def __initLayout(self):
        self.settingLabel.move(36, 30)
        self.settingLabel.setObjectName('settingLabel')

        # Add cards to groups
        self.profileGroup.addSettingCard(self.profileCard)
        
        self.videoGroup.addSettingCard(self.maxSizeCard)
        self.videoGroup.addSettingCard(self.bitrateCard)
        self.videoGroup.addSettingCard(self.fpsCard)
        self.videoGroup.addSettingCard(self.codecCard)
        
        self.controlGroup.addSettingCard(self.turnScreenOffCard)
        self.controlGroup.addSettingCard(self.stayAwakeCard)
        
        self.windowGroup.addSettingCard(self.alwaysOnTopCard)
        self.windowGroup.addSettingCard(self.fullscreenCard)
        self.windowGroup.addSettingCard(self.fixAspectRatioCard)
        
        # Add groups to layout
        self.expandLayout.setSpacing(28)
        self.expandLayout.setContentsMargins(36, 10, 36, 0)
        self.expandLayout.addWidget(self.profileGroup)
        self.expandLayout.addWidget(self.videoGroup)
        self.expandLayout.addWidget(self.controlGroup)
        self.expandLayout.addWidget(self.windowGroup)

    def onProfileChanged(self, profile):
        """ Handle profile change """
        isCustom = profile == "Custom"
        
        # Enable/Disable cards based on profile
        self.maxSizeCard.setEnabled(isCustom)
        self.bitrateCard.setEnabled(isCustom)
        self.fpsCard.setEnabled(isCustom)
        self.codecCard.setEnabled(isCustom)
        
        if profile == "Fast":
            cfg.set(cfg.videoMaxSize, 1024)
            cfg.set(cfg.videoBitrate, 4)
            cfg.set(cfg.videoFps, 60)
            cfg.set(cfg.videoCodec, "h264")
        elif profile == "High Quality":
            cfg.set(cfg.videoMaxSize, 0) # Native
            cfg.set(cfg.videoBitrate, 16)
            cfg.set(cfg.videoFps, 60) # Or 0 for max
            cfg.set(cfg.videoCodec, "h265")
