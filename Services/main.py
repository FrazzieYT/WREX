import os
import sys
import threading
import time
import json
import ctypes
import winreg
from datetime import datetime
from tkinter import *
from tkinter import scrolledtext, messagebox, filedialog

from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

RUN_KEY = r"Software\Microsoft\Windows\CurrentVersion\Run"
APP_NAME = "MySystemMonitor"

DEFAULT_MONITOR_PATHS = [
    os.environ.get("ProgramFiles", "C:\\Program Files"),
    os.environ.get("ProgramFiles(x86)", "C:\\Program Files (x86)"),
    os.environ.get("SystemRoot", "C:\\Windows"),
    os.environ.get("APPDATA", ""),
    os.environ.get("LOCALAPPDATA", ""),
]

REGISTRY_KEYS = [
    r"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    r"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
]

STATE_FILE = "registry_snapshot.json"

def is_admin():
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

def add_to_startup():
    try:
        key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, RUN_KEY, 0, winreg.KEY_SET_VALUE)
        exe_path = sys.executable if getattr(sys, 'frozen', False) else __file__
        if not getattr(sys, 'frozen', False):
            exe_path = f'"{sys.executable}" "{__file__}"'
        winreg.SetValueEx(key, APP_NAME, 0, winreg.REG_SZ, exe_path)
        winreg.CloseKey(key)
        return True, "Программа добавлена в автозагрузку"
    except Exception as e:
        return False, f"Ошибка: {e}"

def remove_from_startup():
    try:
        key = winreg.OpenKey(winreg.HKEY_CURRENT_USER, RUN_KEY, 0, winreg.KEY_SET_VALUE)
        winreg.DeleteValue(key, APP_NAME)
        winreg.CloseKey(key)
        return True, "Программа удалена из автозагрузки"
    except FileNotFoundError:
        return False, "Запись в автозагрузке не найдена"
    except Exception as e:
        return False, f"Ошибка: {e}"

def get_registry_snapshot(keys):
    snapshot = {}
    for key_path in keys:
        try:
            key = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, key_path, 0, winreg.KEY_READ)
            index = 0
            while True:
                try:
                    subkey_name = winreg.EnumKey(key, index)
                    subkey = winreg.OpenKey(key, subkey_name)
                    try:
                        display_name, _ = winreg.QueryValueEx(subkey, "DisplayName")
                    except FileNotFoundError:
                        display_name = subkey_name
                    try:
                        version, _ = winreg.QueryValueEx(subkey, "DisplayVersion")
                    except FileNotFoundError:
                        version = ""
                    snapshot[f"{key_path}\\{subkey_name}"] = {
                        "name": display_name,
                        "version": version,
                    }
                    winreg.CloseKey(subkey)
                    index += 1
                except OSError:
                    break
            winreg.CloseKey(key)
        except FileNotFoundError:
            pass
    return snapshot

def save_snapshot(snapshot, filepath=STATE_FILE):
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(snapshot, f, indent=2, ensure_ascii=False)

def load_snapshot(filepath=STATE_FILE):
    if os.path.exists(filepath):
        with open(filepath, 'r', encoding='utf-8') as f:
            return json.load(f)
    return {}

class FileChangeHandler(FileSystemEventHandler):
    def __init__(self, log_callback):
        self.log_callback = log_callback

    def on_any_event(self, event):
        if event.is_directory:
            return
        if os.path.basename(event.src_path).startswith('~') or event.src_path.endswith('.tmp'):
            return
        action = {
            'created': 'Создан',
            'modified': 'Изменён',
            'deleted': 'Удалён',
            'moved': 'Перемещён',
        }.get(event.event_type, event.event_type)
        if event.event_type == 'moved':
            msg = f"{action}: {event.src_path} -> {event.dest_path}"
        else:
            msg = f"{action}: {event.src_path}"
        self.log_callback(msg)

class MonitorThread(threading.Thread):
    def __init__(self, paths=None, monitor_registry=False, log_callback=None, stop_event=None):
        super().__init__()
        self.paths = paths if paths else []
        self.monitor_registry = monitor_registry
        self.log_callback = log_callback
        self.stop_event = stop_event if stop_event else threading.Event()
        self.observer = None
        self.daemon = True

    def run(self):
        if self.paths:
            self.observer = Observer()
            handler = FileChangeHandler(self.log_callback)
            for path in self.paths:
                if os.path.exists(path):
                    self.observer.schedule(handler, path, recursive=True)
                    self.log_callback(f"[+] Наблюдение за {path}")
                else:
                    self.log_callback(f"[!] Путь не существует: {path}")
            self.observer.start()

        if self.monitor_registry:
            self.log_callback("[+] Мониторинг реестра запущен (интервал 60 сек)")
            while not self.stop_event.is_set():
                current = get_registry_snapshot(REGISTRY_KEYS)
                previous = load_snapshot()
                added = set(current.keys()) - set(previous.keys())
                removed = set(previous.keys()) - set(current.keys())
                modified = []
                for key in set(current.keys()) & set(previous.keys()):
                    if current[key] != previous[key]:
                        modified.append(key)

                if added:
                    for k in added:
                        self.log_callback(f"[{datetime.now()}] [РЕЕСТР] Добавлена запись: {current[k]['name']} (версия {current[k]['version']})")
                if removed:
                    for k in removed:
                        self.log_callback(f"[{datetime.now()}] [РЕЕСТР] Удалена запись: {previous[k]['name']} (версия {previous[k]['version']})")
                if modified:
                    for k in modified:
                        self.log_callback(f"[{datetime.now()}] [РЕЕСТР] Изменена запись: {current[k]['name']} (старая версия {previous[k]['version']}, новая {current[k]['version']})")

                save_snapshot(current)
                self.stop_event.wait(60)
                
        if self.paths and not self.monitor_registry:
            self.stop_event.wait()

        if self.observer:
            self.observer.stop()
            self.observer.join()
            self.log_callback("[+] Наблюдение за файлами остановлено")

    def stop(self):
        self.stop_event.set()
        if self.observer:
            self.observer.stop()

class MonitorApp(Tk):
    def __init__(self):
        super().__init__()
        self.title("Монитор изменений")
        self.geometry("800x600")
        self.monitor_thread = None
        self.full_monitor_active = False

        menubar = Menu(self)
        file_menu = Menu(menubar, tearoff=0)
        file_menu.add_command(label="Добавить в автозагрузку", command=self.cmd_add_startup)
        file_menu.add_command(label="Удалить из автозагрузки", command=self.cmd_remove_startup)
        file_menu.add_separator()
        file_menu.add_command(label="Запустить полный мониторинг", command=self.cmd_start_full_monitor)
        file_menu.add_command(label="Остановить мониторинг", command=self.cmd_stop_monitor)
        file_menu.add_separator()
        file_menu.add_command(label="Выход", command=self.quit)
        menubar.add_cascade(label="Файл", menu=file_menu)
        self.config(menu=menubar)

        main_frame = Frame(self)
        main_frame.pack(fill=BOTH, expand=True, padx=10, pady=10)

        top_frame = Frame(main_frame)
        top_frame.pack(fill=X, pady=5)

        Label(top_frame, text="Путь для мониторинга:").pack(side=LEFT)
        self.path_var = StringVar()
        self.path_entry = Entry(top_frame, textvariable=self.path_var, width=50)
        self.path_entry.pack(side=LEFT, padx=5)

        self.browse_btn = Button(top_frame, text="Обзор", command=self.browse_path)
        self.browse_btn.pack(side=LEFT, padx=5)

        self.follow_btn = Button(top_frame, text="Следить", command=self.cmd_start_monitor)
        self.follow_btn.pack(side=LEFT, padx=5)
        
        self.log_text = scrolledtext.ScrolledText(main_frame, wrap=WORD, state='disabled')
        self.log_text.pack(fill=BOTH, expand=True, pady=5)

        self.protocol("WM_DELETE_WINDOW", self.on_close)

    def log(self, message):
        """Вывод сообщения в текстовое поле (потокобезопасно через after)."""
        self.after(0, self._log_append, message)

    def _log_append(self, message):
        self.log_text.config(state='normal')
        self.log_text.insert(END, message + "\n")
        self.log_text.see(END)
        self.log_text.config(state='disabled')

    def browse_path(self):
        path = filedialog.askdirectory(title="Выберите папку для мониторинга")
        if path:
            self.path_var.set(path)

    def cmd_add_startup(self):
        ok, msg = add_to_startup()
        self.log(f"[{datetime.now()}] {msg}")
        if ok:
            messagebox.showinfo("Автозагрузка", msg)

    def cmd_remove_startup(self):
        ok, msg = remove_from_startup()
        self.log(f"[{datetime.now()}] {msg}")
        if ok:
            messagebox.showinfo("Автозагрузка", msg)

    def cmd_start_monitor(self):
        """Запуск мониторинга по указанному пути (только файлы)."""
        if self.monitor_thread and self.monitor_thread.is_alive():
            self.log("[!] Мониторинг уже запущен. Остановите его перед запуском нового.")
            return
        path = self.path_var.get().strip()
        if not path:
            messagebox.showwarning("Внимание", "Укажите путь для мониторинга")
            return
        if not os.path.exists(path):
            messagebox.showerror("Ошибка", "Путь не существует")
            return

        self.log(f"[{datetime.now()}] Запуск мониторинга пути: {path}")
        self.full_monitor_active = False
        self.monitor_thread = MonitorThread(
            paths=[path],
            monitor_registry=False,
            log_callback=self.log
        )
        self.monitor_thread.start()

    def cmd_start_full_monitor(self):
        """Запуск полного мониторинга (системные папки + реестр)."""
        if self.monitor_thread and self.monitor_thread.is_alive():
            self.log("[!] Мониторинг уже запущен. Остановите его перед запуском нового.")
            return

        if not is_admin():
            response = messagebox.askyesno(
                "Права администратора",
                "Для мониторинга системных папок требуются права администратора.\n"
                "Продолжить без прав? (некоторые папки могут быть недоступны)"
            )
            if not response:
                return

        paths = [p for p in DEFAULT_MONITOR_PATHS if p and os.path.exists(p)]
        if not paths:
            self.log("[!] Нет доступных системных папок для мониторинга")
            return

        self.log(f"[{datetime.now()}] Запуск полного мониторинга (системные папки + реестр)")
        self.full_monitor_active = True
        self.monitor_thread = MonitorThread(
            paths=paths,
            monitor_registry=True,
            log_callback=self.log
        )
        self.monitor_thread.start()

    def cmd_stop_monitor(self):
        if self.monitor_thread and self.monitor_thread.is_alive():
            self.monitor_thread.stop()
            self.monitor_thread.join(timeout=2)
            self.log("[+] Мониторинг остановлен по команде пользователя")
            self.full_monitor_active = False
        else:
            self.log("[!] Мониторинг не запущен")

    def on_close(self):
        if self.monitor_thread and self.monitor_thread.is_alive():
            self.log("[*] Остановка мониторинга при выходе...")
            self.monitor_thread.stop()
            self.monitor_thread.join(timeout=3)
        self.destroy()

if __name__ == "__main__":
    app = MonitorApp()
    app.mainloop()