# coding:utf-8
import os
import sys

from PyQt6.QtCore import Qt, QTranslator, QLocale
from PyQt6.QtWidgets import QApplication
from qfluentwidgets import setTheme, Theme, FluentTranslator

from app.common.config import cfg
from app.view.main_window import MainWindow

if __name__ == '__main__':
    # Enable High DPI scaling
    if cfg.get(cfg.dpiScale) == "Auto":
        os.environ["QT_AUTO_SCREEN_SCALE_FACTOR"] = "1"
        os.environ["QT_ENABLE_HIGHDPI_SCALING"] = "1"
        os.environ["QT_SCALE_FACTOR"] = "1"
    else:
        os.environ["QT_ENABLE_HIGHDPI_SCALING"] = "0"
        os.environ["QT_SCALE_FACTOR"] = str(cfg.get(cfg.dpiScale))

    app = QApplication(sys.argv)
    app.setAttribute(Qt.ApplicationAttribute.AA_DontCreateNativeWidgetSiblings)

    # Internationalization
    locale = cfg.get(cfg.language).value
    if locale.language() == QLocale.Language.C: # Or check if Language.AUTO
        locale = QLocale.system()
    
    fluentTranslator = FluentTranslator(locale)
    settingTranslator = QTranslator()
    
    # Use absolute path to ensure translation files are found
    i18n_path = os.path.abspath("app/resource/i18n")
    if settingTranslator.load(locale, "app", ".", i18n_path):
        print(f"Translation loaded successfully for {locale.name()}")
    else:
        print(f"Failed to load translation for {locale.name()} from {i18n_path}")
    
    app.installTranslator(fluentTranslator)
    app.installTranslator(settingTranslator)

    # Set theme
    setTheme(cfg.get(cfg.themeMode))

    w = MainWindow()
    w.show()

    app.exec()
