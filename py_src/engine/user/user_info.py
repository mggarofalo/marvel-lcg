import locale
import time
import platform
import hashlib
from core import *
from typing import TypeAlias
# import psutil
import uuid

class UserInfo:

    SYSTEM_INFO: TypeAlias = Dict[Literal[
        'system',
        'node',
        'release',
        'version',
        'machine',
        'processor',
        'architecture',
        'max_memory_gb',
        'language',
        'time_zone',
    ], str]

    fingerprint     : str
    system_info     : SYSTEM_INFO = {}

    @staticmethod
    def Initialize():

        def get_system_info() -> 'UserInfo.SYSTEM_INFO':
            return {
                'system': platform.system(),
                'node': platform.node(),
                'release': platform.release(),
                'version': platform.version(),
                'machine': platform.machine(),
                'processor': platform.processor(),
                'architecture': str(" ".join(platform.architecture())),
                'max_memory_gb': get_memory(),
                'language': get_language(),
                'time_zone': get_time_zone(),
            }

        def get_memory():
            # total_memory = psutil.virtual_memory().total
            # total_memory_gb = total_memory / (1024 ** 3)
            # return f"{total_memory_gb:.2f}GB"
            return "0.00GB"

        def get_language():
            # Get the default language of the system
            lang, _ = locale.getdefaultlocale()
            return lang if lang else "Unknow"

        def get_time_zone():
            # Get the current time zone
            return time.tzname[0].replace(' Standard Time', '')

        def generate_fingerprint():
            # Generate a unique fingerprint based on device info
            system_info = UserInfo.system_info
            input_string = f"{system_info['node']}|{system_info['processor']}|{system_info['language']}|{system_info['time_zone']}"

            return hashlib.md5(input_string.encode()).hexdigest()

        UserInfo.system_info    = get_system_info()
        UserInfo.fingerprint    = generate_fingerprint()

