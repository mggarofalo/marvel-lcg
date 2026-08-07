import sys

from engine import Engine

if __name__ == "__main__":

    if Engine.Initialize():

        Engine.EngineRun()

        Engine.Shutdown()

        if Engine.exit_code:
            sys.exit(Engine.exit_code)

