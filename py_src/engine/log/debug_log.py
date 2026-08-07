from core import *
from build import Build

class DebugLog:
    def __init__(self, name: str) -> None:

        if not Build.release:
            self.name = name
            self.debug_message: Dict[str, Dict[str, int]] = {}

    def GetLog(self) -> str:

        text = ""
        text += "<table class='debug-table'>"
        text += f"""
            <thead>
                <tr><th>{self.name}</th>
                    <th>Message</th>
                    <th>Call Count</th>
                    <th>Comment</th></tr></thead>
        """
        text += "<tbody>"

        body = ""
        total_cnt = 0

        for category in self.debug_message:

            for sub_type in self.debug_message[category]:
                count = self.debug_message[category][sub_type]
                total_cnt += count

                body += f"""
                    <tr><td>{category}</td>
                        <td>{sub_type}</td>
                        <td>{count}</td>
                        <td></td>
                    </tr>
                """

        # Add a summary row for the category
        text += f"""
            <tr><td><strong>Total</strong></td>
                <td></td>
                <td>{total_cnt}</td>
                <td></td>
            </tr>
        """

        text += body
        text += "</tbody></table>"

        return text

        # text = f"{self.name}\n"
        # for message in self.debug_message:
        #     text += f"{message}:\n"
        #     for counter in self.debug_message[message]:
        #         text += f"\t{counter}: {self.debug_message[message][counter]}\n"
        # return text

    def AddLog(self, catogory: str, message: str):
        if not Build.release:
            if catogory not in self.debug_message:
                self.debug_message[catogory] = {}
            if message not in self.debug_message[catogory]:
                self.debug_message[catogory][message] = 0
            self.debug_message[catogory][message] += 1

