# coding:utf-8
import sys
from enum import Enum

from PyQt6.QtCore import QLocale
from qfluentwidgets import (qconfig, QConfig, ConfigItem, OptionsConfigItem, BoolValidator,
                            OptionsValidator, Theme, FolderValidator, EnumSerializer,
                            RangeConfigItem, RangeValidator, ColorConfigItem, ColorValidator)


class Language(Enum):
    """ Language enumeration """
    CHINESE = QLocale(QLocale.Language.Chinese, QLocale.Country.China)
    ENGLISH = QLocale(QLocale.Language.English)

class LanguageSerializer(EnumSerializer):
    """ Language serializer """

    def serialize(self, language):
        return language.value.name()

    def deserialize(self, value: str):
        # Try to find matching language
        for lang in Language:
            if lang.value.name() == value:
                return lang
        return Language.CHINESE

class Config(QConfig):
    """ Config of application """
    
    # Main window config
    micaEnabled = ConfigItem("MainWindow", "MicaEnabled", True, BoolValidator())
    dpiScale = OptionsConfigItem(
        "MainWindow", "DpiScale", "Auto", OptionsValidator([1, 1.25, 1.5, 1.75, 2, "Auto"]), restart=True)
    language = OptionsConfigItem(
        "MainWindow", "Language", Language.CHINESE, OptionsValidator(Language), LanguageSerializer(Language), restart=True)
    themeMode = OptionsConfigItem(
        "MainWindow", "ThemeMode", Theme.LIGHT, OptionsValidator([Theme.LIGHT, Theme.DARK]), EnumSerializer(Theme))
    themeColor = ColorConfigItem("MainWindow", "ThemeColor", "#009faa") # Default blue-ish

    # Scrcpy Presets
    scrcpyProfile = OptionsConfigItem(
        "Scrcpy", "Profile", "High Quality", OptionsValidator(["Fast", "High Quality", "Custom"]))

    # Scrcpy Video Settings
    videoMaxSize = OptionsConfigItem(
        "Scrcpy", "MaxSize", 0, OptionsValidator([0, 1920, 1600, 1280, 1024, 800])) # 0 means native
    videoBitrate = RangeConfigItem("Scrcpy", "Bitrate", 8, RangeValidator(1, 50)) # Mbps
    videoFps = OptionsConfigItem(
        "Scrcpy", "FPS", 0, OptionsValidator([0, 30, 60, 90, 120])) # 0 means auto/uncapped
    videoCodec = OptionsConfigItem(
        "Scrcpy", "Codec", "h264", OptionsValidator(["h264", "h265", "av1"]))
    
    # Scrcpy Control Settings
    turnScreenOff = ConfigItem("Scrcpy", "TurnScreenOff", False, BoolValidator())
    stayAwake = ConfigItem("Scrcpy", "StayAwake", False, BoolValidator())
    
    # Window
    alwaysOnTop = ConfigItem("Scrcpy", "AlwaysOnTop", False, BoolValidator())
    fullscreen = ConfigItem("Scrcpy", "Fullscreen", False, BoolValidator())
    fixAspectRatio = ConfigItem("Scrcpy", "FixAspectRatio", False, BoolValidator()) # 16:9 Lock

cfg = Config()
qconfig.load('config/config.json', cfg)
