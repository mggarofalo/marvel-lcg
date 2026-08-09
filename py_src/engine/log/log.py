import os
from core.lib import Time
from core import *
from colorama import Fore, Style

from build import Build
from engine.config import ConfigVariables

CATEGORY_NAME = "LOG"

SHOW_SILENT_LOG_CATEGORIES  = ConfigVariables.ListStr('show_silent_log_categories', [])
HIDDEN_LOG_CATEGORIES       = ConfigVariables.ListStr('hidden_log_categories', [])
CRASH_FILE                  = ConfigVariables.File('crash_file', "crash.log")
DISPLAY_SOUND_NAME          = ConfigVariables.Bool('display_sound_name', False)

def PrintUtf8(log_text: str) -> None:
    try:
        print(log_text, end="")
    except Exception as e:
        print(f"Error printing log: {e}")

class LogHelper:

    CATEGORY_COLOR: Dict['Log.CATEGORY', str] = {
        "WEB"       : f"{Fore.LIGHTMAGENTA_EX}",
        "EFFECT"    : f"{Fore.GREEN}",
        "CHEAT"     : f"{Fore.CYAN}",
        "VERSION"   : f"{Fore.BLUE}",
        "STATISTICS": f"{Fore.BLUE}",
        "ENGINE"    : f"{Fore.LIGHTBLUE_EX}",
    }

    LEVEL_COLORS: Dict['Log.LEVELS', str] = {
        'Info'      : f"{Fore.GREEN}",
        'Error'     : f"{Fore.RED}",
        'Debug'     : f"{Fore.LIGHTBLACK_EX}",
        'DebugInfo' : f"{Fore.LIGHTBLACK_EX}",
        'Warn'      : f"{Fore.YELLOW}",
        'Failed'    : f"{Style.BRIGHT}{Fore.RED}",
        'Hack'      : f"{Fore.LIGHTBLUE_EX}",
    }

    LEVELS_FOR_STATISTICS: List['Log.LEVELS'] = ['Warn', 'Error', 'Failed']

    @staticmethod
    def StatLog(category: 'Log.CATEGORY', level: 'Log.LEVELS', text: str):
        """Count one log line. Runs on every build.

        `Log.HasError` is the only reader, and it is a correctness signal
        rather than a debug report: `BotRunner.Verify` uses it to decide whether
        a generated scene may enter the corpus, and `tools/spec/harness.py` uses
        it to catch a spec case that "passed" over an exception the engine
        swallowed. Gating this on `Build.release` -- which `build.py` hardcodes
        true -- made both of those always pass. See MARVEL-65.

        In a release build `text` is "" because no caller stack is collected, so
        the counts degenerate to one entry per category and level. That is all
        `HasError` reads. `ReportStats`, which wants the per-call-site detail,
        is only ever called under `not Build.release`.
        """
        def save(c: 'Log.CATEGORY'):
            if c not in Log.log_statistics:
                Log.log_statistics[c] = {}
            if level not in Log.log_statistics[c]:
                Log.log_statistics[c][level] = {}
            if text not in Log.log_statistics[c][level]:
                Log.log_statistics[c][level][text] = 0
            Log.log_statistics[c][level][text] += 1
        save(category)
        save("ALL")


    @staticmethod
    def Print(log_text: str, end: str="") -> None:
        log_text = log_text + end
        Log.all_log_text += log_text
        PrintUtf8(log_text)

    @staticmethod
    def PrintInternal(category: 'Log.CATEGORY', level: 'Log.LEVELS', infos: str) -> None:
        color_level = LogHelper.LEVEL_COLORS[level]
        color_category = LogHelper.CATEGORY_COLOR[category] if category in LogHelper.CATEGORY_COLOR else color_level
        infos = infos.replace(Fore.RESET, color_level)

        log_text = ""

        color_time = f"{Fore.LIGHTBLACK_EX}"
        time = f"{Log.GetGameTime():.3f}"

        for info_line in infos.splitlines():
            category_name = f"{category[0:3]}"
            level_name = str(level).upper()
            if category != "TEST":
                Log.all_log_text += f'[{category_name}] {time} <{level_name}> {info_line}\n'

            if Build.release:
                log_text += f'{color_level}<{level_name[0:1]}> {info_line}{Style.RESET_ALL}\n'
            else:
                log_text += f'{color_category}[{category_name}] {color_time}{time} | {color_level}{info_line}{Style.RESET_ALL}\n'

        PrintUtf8(log_text)

    @staticmethod
    def PrintLog(category: 'Log.CATEGORY', level: 'Log.LEVELS', *info: object) -> None:
        if Build.release:
            show_caller = False
        elif level in ['Hack'] + LogHelper.LEVELS_FOR_STATISTICS:
            show_caller = True
        else:
            show_caller = False

        line = ""
        if show_caller:
            line = GetCallStack(3)

        # Count before the display filters, not after. Whether a category is
        # hidden is a presentation choice, and it must not decide whether an
        # error is *detectable* -- `-bot` expands to `-hidden_log_categories
        # CONTROLLER WEB VERSION STATISTICS`, so an error logged in any of those
        # was invisible to `Log.HasError` and therefore to the corpus
        # verification gate. See MARVEL-65.
        if level in LogHelper.LEVELS_FOR_STATISTICS:
            LogHelper.StatLog(category, level, line)

        # Hide debug info in release version
        if category in HIDDEN_LOG_CATEGORIES.value:
            return

        infos = " ".join(str(x) for x in info)
        if line:
            infos = line + "\n" + infos

        LogHelper.PrintInternal(category, level, infos)

class Log:

    CATEGORY = Literal["UPLOAD", "GAME", "WEB", "ENGINE", "STATISTICS", "SCENE", "TEST", "CHEAT", "EDITOR", "CONTROLLER", "CACHE", "EFFECT", "SELECTOR", "REPLAY", "PLAYER", "SENDER", "RENDER", "VERSION", "THREADS", "LOG", "NOTIFY", "LOAD", "DEVICE_MANAGER", "WEB_DEVICE_MANAGER", "JSON", "CONTROLLER_MANAGER", "TASK", "COMMAND", "PUZZLE", "ALL", "SERVER_CONFIG", "SYNC", "NEW", "RANDOM", "CHECK_NEW_VERSION", "UNIT_TEST", "FILE", "CONSOLE", "JOB", "GAME_STATE", "SKIP", "SESSION", "FAST_UNDO", "MESSAGE", "UNDO_HANDLE", "BOT"]

    LEVELS = Literal['Warn', 'Error', 'Info', 'Debug', 'DebugInfo', 'Failed', 'Hack']

    log_statistics: Dict[CATEGORY, Dict[LEVELS, Dict[str, int]]] = {}

    all_log_text: str = ""
    start_time: float = 0

    # Notified for every exception `OnCrash` handles, absorbed or not. Nothing
    # in the engine sets it; the headless bot installs one so a self-play run
    # turns an absorbed exception into a replayable artefact instead of a
    # traceback nobody can act on. See `bot/crash.py` and MARVEL-12.
    crash_observer: 'Callable[[Log.CATEGORY, Exception], None]|None' = None

    ################################################################################
    #
    @staticmethod
    def Print(*info: object, end: str="\n") -> None:
        infos = " ".join(str(x) for x in info)
        LogHelper.Print(infos, end=end)

    ################################################################################
    #
    @staticmethod
    def Info(category: 'Log.CATEGORY', *info: object) -> None:
        LogHelper.PrintLog(category, 'Info', *info)

    @staticmethod
    def PrintGameInfo(*info: object, sound_name: str, render_id: int|None=None, code_info: str="") -> None:
        from core.utility.func import ROOT_DIR
        infos = " ".join(str(x) for x in info)
        info_lines: List[str] = []
        for info_line in infos.splitlines():
            info_lines.append(f"> {info_line}")
        if sound_name and DISPLAY_SOUND_NAME.value:
            info_lines[-1] += f"  ({Fore.BLACK}{sound_name}{Fore.RESET})"
        if render_id != None:
            info_lines[0] = f"> {render_id:3}: {info_lines[0][2:]}"
        if code_info:
            code_info = os.path.relpath(code_info, ROOT_DIR)
            info_lines[-1] = f"{info_lines[-1]}  {Fore.LIGHTBLACK_EX}{code_info}{Fore.RESET}"
        LogHelper.Print("\n".join(info_lines) + "\n", end="")

    @staticmethod
    def PrintNull(category: 'Log.CATEGORY', *info: object) -> None:
        pass

    @staticmethod
    def Hack(category: 'Log.CATEGORY', *info: object) -> None:
        LogHelper.PrintLog(category, 'Hack', *info)

    @staticmethod
    def Debug(category: 'Log.CATEGORY', *info: object):
        LogHelper.PrintLog(category, 'Debug', *info)

    @staticmethod
    def DebugInfo(category: 'Log.CATEGORY', *info: object):
        LogHelper.PrintLog(category, 'DebugInfo', *info)

    @staticmethod
    def DebugSilent(category: Literal["SYNC", "DEVICE_MANAGER", "WEB_DEVICE_MANAGER", "LOG", "NEW", "RANDOM", "JOB", "GAME_STATE", "SKIP", "FAST_UNDO", "MESSAGE", "UNDO_HANDLE"], *info: object):
        if category in SHOW_SILENT_LOG_CATEGORIES.value:
            LogHelper.PrintLog(category, 'Debug', *info)
        pass

    if Build.release:
        Hack        = PrintNull
        Debug       = PrintNull
        # DebugInfo   = PrintNull
        DebugSilent = PrintNull

    # No recorded in the `Log.all_log_text`
    @staticmethod
    def Test(*info: object):
        from game.test import Test
        if Test.IsInTesting():
            infos = " ".join(str(x) for x in info)
            PrintUtf8(infos)

    @staticmethod
    def Warn(category: 'Log.CATEGORY', *info: object):
        LogHelper.PrintLog(category, 'Warn', *info)

    @staticmethod
    # Error
    def Assert(category: 'Log.CATEGORY', *info: object):
        LogHelper.PrintLog(category, 'Error', *info)

    @staticmethod
    def FailedTrace(category: 'Log.CATEGORY', exc: Exception, *, no_take_as_error: bool=False) -> str:
        import traceback
        info = traceback.format_exc()
        if no_take_as_error:
            LogHelper.PrintLog(category, 'Debug', info)
        else:
            LogHelper.PrintLog(category, 'Failed', info)
        return info

    @staticmethod
    def OnCrash(category: 'Log.CATEGORY', exc: Exception, name: str, function: Callable[..., Any]|None,
                # *,
                # no_take_as_error: bool=False,
                # no_save_crash: bool=False,
                ) -> str:
        from engine import Engine

        if function:
            function_name = GetFuncName(function)
        else:
            function_name = ""

        info = Log.FailedTrace(category, exc)
        Log.Debug(category, name, function_name)
        # Log.Notify(CATEGORY_NAME, "Error", function_name)

        if str(exc) == 'maximum recursion depth exceeded in comparison':
            exit()

        if isinstance(exc, EngineIntegrityError):
            # This one says the run has already produced something that must
            # not be trusted, so absorbing it would turn a loud failure into a
            # corrupt artefact that looks clean. Re-raise before `SaveCrash`,
            # which would otherwise write the very state we are refusing to
            # keep. The traceback is already logged above. See MARVEL-32.
            raise exc

        # Everything from here down is the absorbing path: the exception stops
        # here and the game carries on. That is the case a self-play run cannot
        # see any other way, so tell the observer before the frame is gone. It
        # runs after the integrity check on purpose -- an integrity error is
        # re-raised and its caller captures it, and observing it here would
        # write the state that branch exists to refuse. See MARVEL-12.
        Log.NotifyCrashObserver(category, exc)

        Engine.SaveCrash()

        if not Build.release:
            raise
        else:
            return info

    @staticmethod
    def NotifyCrashObserver(category: 'Log.CATEGORY', exc: Exception) -> None:
        """Hand the exception to whoever is collecting them, if anyone is.

        A crash reporter must never become the crash: this runs inside an
        `except` block in the middle of a game, so an observer that raises
        would replace a bug we can diagnose with one we cannot.
        """
        observer = Log.crash_observer
        if observer == None:
            return
        try:
            observer(category, exc)
        except Exception as observer_exc:
            Log.crash_observer = None
            Log.Warn(CATEGORY_NAME,
                f"Crash observer raised {type(observer_exc).__name__}: {observer_exc}. "
                "Uninstalled it; crashes will no longer be collected this run.")

    ################################################################################
    #
    @staticmethod
    def SaveCrashLog():
        from engine.file import FileManager

        file_name = CRASH_FILE.value
        Log.Info(CATEGORY_NAME, f"Save {file_name}")
        with FileManager.OpenFile(file_name, write=True) as file:
            file.Write(Log.all_log_text)

    ################################################################################
    #
    @staticmethod
    def ReportStats() -> None:
        # Log.Print('\n--- Stats ---')
        Log.Print('\nLog statistics:')

        for category in Log.log_statistics:
            if category == "ALL":
                continue
            for level in LogHelper.LEVELS_FOR_STATISTICS:
                if level not in Log.log_statistics[category]:
                    continue
                for text in Log.log_statistics[category][level]:
                    count = Log.log_statistics[category][level][text]
                    path = (text + " ").ljust(60, "-")
                    # Log.Print(f'{path} {count}', level)
                    LogHelper.PrintInternal(category, level, f'{path} {count}')
        # Log.Print('--- ----- ---')

    @staticmethod
    def Setup():
        Log.log_statistics = {}
        # Log.all_log_text = ""

    @staticmethod
    def ResetTime():
        Log.start_time = Time.GetTime()
        Log.DebugSilent(CATEGORY_NAME, "Time reset")

    @staticmethod
    def GetGameTime() -> float:
        if Log.start_time == 0:
            return 0
        return Time.GetTime() - Log.start_time

    @staticmethod
    def HasError(category: 'Log.CATEGORY|None'=None, warn: bool=False, error: bool=False) -> bool:
        if category == None:
            category = "ALL"
        if category not in Log.log_statistics:
            return False

        def check_has_key(level: 'Log.LEVELS'):
            if level not in Log.log_statistics[category]:
                return False
            return len(Log.log_statistics[category][level]) > 0

        if error:
            if check_has_key('Error'):
                return True
            if check_has_key('Failed'):
                return True
        if warn:
            if check_has_key('Warn'):
                return True
        return False

