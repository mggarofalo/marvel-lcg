from typing import Final
from core import *
import requests
from html.parser import HTMLParser
from engine.lib import Ver
from engine.log import Log
from colorama import Fore

CATEGORY_NAME = "CHECK_NEW_VERSION"

class CheckForNewVersion():

    check_url: Final = 'https://irefrixs.itch.io/marvel-lcg'

    class DownloadLinkParser(HTMLParser):
        def __init__(self) -> None:
            super().__init__()
            self.upload_names: List[str] = []
            self.in_upload_name = False

        @override
        def handle_starttag(self, tag: str, attrs: List[Tuple[str, str|None]]) -> None:
            if tag == 'div':  # Assuming the upload names are in <div> tags
                for attr in attrs:
                    if attr[0] == 'class' and attr[1] and 'upload_name' in attr[1]:
                        self.in_upload_name = True
                        break

        @override
        def handle_endtag(self, tag: str) -> None:
            if tag == 'div' and self.in_upload_name:
                self.in_upload_name = False

        @override
        def handle_data(self, data: str) -> None:
            if self.in_upload_name:
                self.upload_names.append(data.strip())

    @staticmethod
    def Check():

        current_ver = Ver.version
        # current_ver = Ver("0.5.9.4")

        # Make a POST request to the URL
        # Log.Info(CATEGORY_NAME, "Checking new version...")
        try:
            response: requests.Response = requests.post(CheckForNewVersion.check_url)
        except:
            Log.Info(CATEGORY_NAME, f"{Fore.RED}Version check failed{Fore.RESET}")
            return

        if response.status_code != 200:
            Log.Info(CATEGORY_NAME, "Error fetching the page:", response.status_code, response.reason)
            return

        # Parse the HTML content
        parser = CheckForNewVersion.DownloadLinkParser()
        parser.feed(response.text)

        if not parser.upload_names:
            Log.Info(CATEGORY_NAME, "HTML parsing error.")
            return

        def find_new_version():
            import re
            for name in parser.upload_names:
                match = re.search(r'v(\d+\.\d+\.\d+\.\d+)', name)
                if match:
                    version = match.group(1)
                    if Ver(version) > current_ver:
                        return version
        
        version = find_new_version()
        if version:
            Log.Info(CATEGORY_NAME, f"New version available: {Fore.YELLOW}{version}{Fore.RESET}")
        else:
            Log.Info(CATEGORY_NAME, f"This game is already up to date.")

