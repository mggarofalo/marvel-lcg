import re
from build import Build
import os

def IncreaseVersion():
    build_file_name = "./build.py"
    version = ""

    with open(build_file_name, "r") as f:
        file_text = f.read()
        version = str(Build.BUILD + 1)
        file_text = re.sub(r"BUILD = (\d+)", "BUILD = " + version, file_text)

    with open(build_file_name, "w") as f:
        f.write(file_text)

    assert version != "", f"{version=}"

    os.system(f"git add {build_file_name}")
    os.system(f'git commit -m "Package version {Build.MAJOR}.{Build.MINOR}.{Build.PATCH}.{version}"')


if __name__ == "__main__":
    IncreaseVersion()

