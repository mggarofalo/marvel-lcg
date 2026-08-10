import os
import sys

class System:

    # `title` and `pause` are cmd.exe builtins. Elsewhere the shell has no such
    # commands, so every engine boot wrote `sh: 1: title: not found` to stderr
    # -- harmless to the game, but the determinism and replay probes capture a
    # child process's stderr and report it when a check fails, so it turned
    # every Linux probe failure into a misleading one. See MARVEL-72.
    IS_WINDOWS = sys.platform == 'win32'

    @staticmethod
    def Run(cmd: str):
        os.system(cmd)

    @staticmethod
    def SetTitle(title: str):
        if not System.IS_WINDOWS:
            return
        System.Run(f'title {title}')

    @staticmethod
    def Pause():
        if not System.IS_WINDOWS:
            return
        System.Run('pause')

    @staticmethod
    def Sleep(secs: float):
        from time import sleep
        return sleep(secs)

