import os

class Build:
    release = "RELEASE" in os.environ
    release = True

    # Version
    MAJOR = 0
    MINOR = 5
    PATCH = 9
    BUILD = 201

