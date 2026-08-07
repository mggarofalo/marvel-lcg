from typing import Dict, Set
import unittest
from build_marvel import IncreaseVersion
from colorama import Fore, Back, Style

class TestMain(unittest.TestCase):

    # Just use as a work, to help me increase the version number
    def test_IncreaseVersion(self):
        IncreaseVersion()

    def test_zip_cards(self):
        import os
        import zipfile
        import time

        # Define the paths
        folders = [
            './cards/pack/angel/',
            './cards/pack/angel/angel/',
            './cards/pack/ant/',
            './cards/pack/ant/ant_man/',
            './cards/pack/bkw/',
            './cards/pack/cap/',
            './cards/pack/core/',
            './cards/pack/cw/',
            './cards/pack/cw/hulkling/',
            './cards/pack/cw/tigra/',
            './cards/pack/cyclops/',
            './cards/pack/cyclops/cyclops/',
            './cards/pack/deadpool/',
            './cards/pack/deadpool/deadpool/',
            './cards/pack/drax/',
            './cards/pack/drax/drax/',
            './cards/pack/drs/',
            './cards/pack/falcon/',
            './cards/pack/gam/',
            './cards/pack/gambit/gambit/',
            './cards/pack/hlk/',
            './cards/pack/hlk/hulk/',
            './cards/pack/iceman/',
            './cards/pack/iceman/frostbite/',
            './cards/pack/ironheart/',
            './cards/pack/jubilee/',
            './cards/pack/jubilee/jubilee/',
            './cards/pack/msm/',
            './cards/pack/msm/ms_marvel/',
            './cards/pack/mts/',
            './cards/pack/mut_gen/',
            './cards/pack/ncrawler/',
            './cards/pack/ncrawler/nightcrawler/',
            './cards/pack/nebu/',
            './cards/pack/nebu/nebula/',
            './cards/pack/next_evol/',
            './cards/pack/nova/',
            './cards/pack/nova/nova/',
            './cards/pack/phoenix/',
            './cards/pack/phoenix/phoenix/',
            './cards/pack/psylocke/',
            './cards/pack/qsv/',
            './cards/pack/qsv/quicksilver/',
            './cards/pack/rogue/',
            './cards/pack/scw/',
            './cards/pack/scw/scarlet_witch/',
            './cards/pack/silk/',
            './cards/pack/sm/',
            './cards/pack/spdr/',
            './cards/pack/spiderham/',
            './cards/pack/stld/',
            './cards/pack/stld/star_lord/',
            './cards/pack/storm/',
            './cards/pack/thor/',
            './cards/pack/thor/thor/',
            './cards/pack/trors/',
            './cards/pack/valk/',
            './cards/pack/vision/',
            './cards/pack/vision/vision/',
            './cards/pack/vnm/',
            './cards/pack/vnm/venom/',
            './cards/pack/warm/',
            './cards/pack/warm/war_machine/',
            './cards/pack/winter/',
            './cards/pack/wolv/',
            './cards/pack/wsp/',
            './cards/pack/wsp/wasp/',
            './cards/pack/x23/',
            './cards/pack/x23/x_23/',
        ]

        # Output zip file name

        from build import Build
        output_zip = f"./cards-{Build.MAJOR}.{Build.MINOR}.{Build.PATCH}.{Build.BUILD}.zip"

        # Target timestamp (for example, a uniform date-time)
        uniform_timestamp = time.mktime(time.strptime("2022-01-01 00:00:00", "%Y-%m-%d %H:%M:%S"))

        # Create a zip file
        with zipfile.ZipFile(output_zip, 'w', zipfile.ZIP_DEFLATED) as zipf:
            for folder in folders:
                for file in os.listdir(folder):
                    file_path = os.path.join(folder, file)

                    # Only consider files (not directories) and exclude __init__.py
                    if os.path.isfile(file_path) and file != '__init__.py' and file != 'campaign.py':
                        # Add file to the zip file
                        zipf.write(file_path, os.path.relpath(file_path, os.path.dirname(folder)))
                        
                        # Set the uniform timestamp for the added file
                        zip_info = zipf.getinfo(os.path.relpath(file_path, os.path.dirname(folder)))
                        zip_info.date_time = time.localtime(uniform_timestamp)[:6]

        print(f"Zipped files into {output_zip}")

