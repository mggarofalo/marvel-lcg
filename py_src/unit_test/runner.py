from core import *
from core.lib import Time
import sys
from engine.log import Log
from core.utility.git import Git
from engine.config import ConfigVariables

TEST_RESULT_FILE = ConfigVariables.File('test_result_file', 'test_results.log')
CATEGORY_NAME = "UNIT_TEST"

class TestRunner:

    @staticmethod
    def Execute(func: Callable[..., Any], *args: Any, **kwargs: Any):
        Log.all_log_text = ""

        separator = "-" * 40  # Customize the separator as needed
        Log.Print(separator)
        Log.Print(f"{sys.argv=}")
        Log.Print(f"{Time.Format()}")
        Log.Print(f"{GetCallStack(3)}")
        # Log.Print(f'{Build.release=}')
        # Log.Print(f'{IsVSDebug()=}')
        Log.Print(f"{Git.GetHashAndMessage()}")
        Log.Print(f"{Git.ListFiles()=}")

        func(*args, **kwargs)

        output_file = TEST_RESULT_FILE.value
        if output_file:
            with open(output_file, "a") as f:
                f.write(Log.all_log_text + "\n\n")  # Append to file

            Log.Info(CATEGORY_NAME, f"Save: {output_file}")

