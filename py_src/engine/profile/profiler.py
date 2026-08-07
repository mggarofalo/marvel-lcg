from core import *
from core.lib import Time
import cProfile
from typing import TypeAlias
from build import Build
from engine.config import ConfigVariables

ENABLE_PROFILE_CATEGORY     = ConfigVariables.ListStr('enable_profile_category', [])
EXCLUDE_PROFILE_FUNCTIONS   = ConfigVariables.ListStr('exclude_profile_functions', [])
EXCLUDE_PROFILE_FILES       = ConfigVariables.Files('exclude_profile_files', [])

class ProfileManager:

    # category, name
    instances: Dict[str, Dict[str, 'Profiler']] = {}

    @staticmethod
    def New(name: str, *, category: str="None") -> 'Profiler':
        if category not in ProfileManager.instances:
            ProfileManager.instances[category] = {}

        if name in ProfileManager.instances[category]:
            ProfileManager.instances[category][name].End()

        profiler = Profiler(name)
        ProfileManager.instances[category][name] = profiler

        return profiler

    @staticmethod
    def Get(name: str, *, category: str="None") -> 'Profiler':
        return ProfileManager.instances[category][name]

    @staticmethod
    def GetDebugMessage() -> str:
        text = ""

        # Profilers.data[profiler.category] = OrderedDict(
        #     [(profiler.name, profiler.info)] + 
        #     [(k, v) for k, v in Profilers.data[profiler.category].items() if k != profiler.name]
        # )

        for category in ProfileManager.instances:
            total_cnt = 0
            total_time = 0

            text += "<table class='debug-table'>"
            text += f"""
                <thead>
                    <tr><th>{category}</th>
                        <th>Call Count</th>
                        <th>Call Time</th>
                        <th>AVE Time</th>
                        <th>Comment</th></tr></thead>
            """
            text += "<tbody>"
            body = ""

            for sub_type in ProfileManager.instances[category]:
                profile_info = ProfileManager.instances[category][sub_type].info
                if profile_info.call_count == 0:
                    continue

                total_cnt += profile_info.call_count
                total_time += profile_info.call_time

                ave = profile_info.call_time / profile_info.call_count
                comment = profile_info.comment

                body += f"""
                    <tr><td>{sub_type}</td>
                        <td>{profile_info.call_count}</td>
                        <td>{profile_info.call_time:.4f}</td>
                        <td>{ave:.4f}</td>
                        <td>{comment}</td>
                    </tr>
                """

            # Add a summary row for the category
            text += f"""
                <tr><td><strong>Total</strong></td>
                    <td>{total_cnt}</td>
                    <td>{total_time:.4f}</td>
                    <td>{total_time / total_cnt:.4f}</td>
                    <td></td>
                </tr>
            """
            text += body
            text += "</tbody></table>"

        return text


class Profile:

    T = TypeVar("T")

    PROFILE_CATEGORY: TypeAlias = Literal["None", "Effect", "EventManager.Filter", "EventManager", "Message", "Render", "Test", "Game"]

    @staticmethod
    def Run(func: Callable[..., T],
            *args: Any,
            profile_name: str,
            profile_category: PROFILE_CATEGORY="None",
            **kwargs: Any) -> T:
        return func(*args, **kwargs)

    @staticmethod
    def RunProfile(func: Callable[..., T],
            *args: Any,
            profile_name: str,
            profile_category: str="None",
            **kwargs: Any) -> T:
        if profile_category not in ENABLE_PROFILE_CATEGORY.value:
            result = func(*args, **kwargs)
        else:
            profiler = ProfileManager.New(profile_name, category=profile_category)
            profiler.Start()
            result = func(*args, **kwargs)
            profiler.End()
        return result

    if not Build.release:
        Run = RunProfile

    @staticmethod
    def Get(name: str, *, category: str="None") -> 'Profiler':
        return ProfileManager.Get(name, category=category)

    @staticmethod
    def GetDebugMessage() -> str:
        return ProfileManager.GetDebugMessage()


class Profiler:

    class ProfileInfo:
        def __init__(self) -> None:
            self.comment    = ""
            self.call_count : int   = 0
            self.call_time  : float = 0
            self.start_time : float = 0

    def __init__(self, name: str, *, category: str="None") -> None:
        self.category   = category
        self.name       = name
        self.info       = Profiler.ProfileInfo()
        self.cprofiler  = cProfile.Profile()

    def __repr__(self) -> str:
        return f"{self.name} {self.category} {self.info.call_count}"

    def Start(self) -> None:
        self.info.start_time = Time.GetTime()
        self.info.call_count += 1
        self.cprofiler.enable()

    def End(self, comment: str|int|None=None) -> None:
        end = Time.GetTime()

        self.info.call_time += end - self.info.start_time
        if comment != None:
            self.info.comment = str(comment)

        self.cprofiler.disable()

    def Print(self) -> str:
        import pstats, io

        profiler = self.cprofiler
        stream = io.StringIO()
        stats = pstats.Stats(profiler, stream=stream).strip_dirs().sort_stats('cumulative')

        stats = getattr(stats, 'stats', {})
        # Filter the stats to include only those with a total time (tottime) of at least 0.001 seconds
        filtered_stats = [stat for stat in stats.items() if stat[1][2] >= 0.001]

        # Filter out the functions where percall (tottime) < 0.001
        filtered_stats = [
            stat for stat in filtered_stats 
            if (stat[1][2] / stat[1][0] if stat[1][0] != 0 else 0) >= 0.000_001
        ]

        # TODO: Filter out the functions to exclude
        # filtered_stats = [stat for stat in filtered_stats if not hasattr(stat[0][2], '_no_profile')]

        # Filter out the files and functions to exclude
        filtered_stats = [stat for stat in filtered_stats if stat[0][0] not in EXCLUDE_PROFILE_FILES.value]
        filtered_stats = [stat for stat in filtered_stats if stat[0][2] not in EXCLUDE_PROFILE_FUNCTIONS.value]

        # Prepare the output string in a format similar to `print_stats`
        output = io.StringIO()
        output.write("   ncalls  tottime  percall  cumtime  percall filename:lineno(function)\n")

        """
        - ncalls:       The number of times the function was called.
        - tottime:      Excluding time spent in calls to sub-functions.
        - percall:      `tottime / ncalls`
        - cumtime:      Including time spent in all sub-functions.
        - percall:      `cumtime / ncalls`
        """

        filtered_stats = sorted(filtered_stats, key=lambda x: x[1][3]/x[1][0], reverse=True)[:40]

        for _, (func, stat) in enumerate(filtered_stats):
            ncalls = stat[0]
            tottime = stat[2]
            percall_tot = tottime / ncalls
            cumtime = stat[3]
            percall_cum = cumtime / ncalls
            output.write(f" {ncalls:>8} {tottime:>8.3f} {percall_tot:>8.3f} {cumtime:>8.3f} {percall_cum:>8.3f} {func[0]}:{func[1]}({func[2]})\n")

        # Print the formatted output
        return output.getvalue()

