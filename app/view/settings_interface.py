# coding:utf-8
from PyQt6.QtCore import Qt, QUrl
from PyQt6.QtGui import QDesktopServices
from PyQt6.QtWidgets import QWidget, QLabel, QVBoxLayout

from qfluentwidgets import (ScrollArea, SettingCardGroup, OptionsSettingCard, ComboBoxSettingCard, 
                            HyperlinkCard, PrimaryPushSettingCard, FluentIcon as FIF, setTheme, TitleLabel,
                            ColorSettingCard, setThemeColor)
from app.common.config import cfg

class SettingsInterface(ScrollArea):
    """ Settings interface """

    def __init__(self, parent=None):
        super().__init__(parent)
        self.scrollWidget = QWidget()
        self.expandLayout = QVBoxLayout(self.scrollWidget)
        
        # Apply style sheet
        self.scrollWidget.setObjectName('scrollWidget')
        self.setObjectName('settingsInterface')
        
        # Make background transparent to show Mica effect or parent background
        self.scrollWidget.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.viewport().setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        self.setStyleSheet("SettingsInterface, #scrollWidget { background-color: transparent; }")
        self.settingLabel = TitleLabel(self.tr("App Settings"), self)

        # Settings Groups
        self.appGroup = SettingCardGroup(self.tr("Application"), self.scrollWidget)
        self.aboutGroup = SettingCardGroup(self.tr("About"), self.scrollWidget)

        # App Items
        self.themeCard = OptionsSettingCard(
            cfg.themeMode,
            FIF.BRUSH,
            self.tr("Theme Mode"),
            self.tr("Change the appearance of the application"),
            texts=[self.tr("Light"), self.tr("Dark")],
            parent=self.appGroup
        )
        self.themeColorCard = ColorSettingCard(
            cfg.themeColor,
            FIF.PALETTE,
            self.tr("Theme Color"),
            self.tr("Change the theme color of the application"),
            self.appGroup
        )
        self.langCard = ComboBoxSettingCard(
            cfg.language,
            FIF.LANGUAGE,
            self.tr("Language"),
            self.tr("Change the language of the application"),
            texts=[self.tr("Chinese"), self.tr("English")],
            parent=self.appGroup
        )

        # About Items
        self.githubCard = HyperlinkCard(
            "https://github.com/Genymobile/scrcpy",
            self.tr("Scrcpy Core"),
            FIF.GITHUB,
            self.tr("Core scrcpy functionality provided by Genymobile"),
            self.tr("View on GitHub"),
            self.aboutGroup
        )
        self.feedbackCard = PrimaryPushSettingCard(
            self.tr("Provide Feedback"),
            FIF.FEEDBACK,
            self.tr("Provide Feedback"),
            self.tr("Help us improve the application"),
            self.aboutGroup
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
        cfg.themeMode.valueChanged.connect(setTheme)
        cfg.themeColor.valueChanged.connect(setThemeColor)
        
        self.feedbackCard.clicked.connect(lambda: QDesktopServices.openUrl(QUrl("https://github.com/Genymobile/scrcpy/issues")))

    def __initLayout(self):
        self.settingLabel.move(36, 30)
        self.settingLabel.setObjectName('settingLabel')

        # Add cards to groups
        self.appGroup.addSettingCard(self.themeCard)
        self.appGroup.addSettingCard(self.themeColorCard)
        self.appGroup.addSettingCard(self.langCard)
        
        self.aboutGroup.addSettingCard(self.githubCard)
        self.aboutGroup.addSettingCard(self.feedbackCard)

        # Add groups to layout
        self.expandLayout.setSpacing(28)
        self.expandLayout.setContentsMargins(36, 10, 36, 0)
        self.expandLayout.addWidget(self.appGroup)
        self.expandLayout.addWidget(self.aboutGroup)
