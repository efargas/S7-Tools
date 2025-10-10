#!/usr/bin/env python3
"""
Quick test script to verify socat profile service functionality.
This helps debug why the profiles.json file is not being created.
"""

import os
import json
from pathlib import Path
import glob

def test_socat_service():
    # Determine the application directory dynamically
    # 1. Check for environment variable S7TOOLS_BUILD_OUTPUT
    build_output_env = os.environ.get("S7TOOLS_BUILD_OUTPUT")
    if build_output_env:
        app_dir = Path(build_output_env)
    else:
        # 2. Search for likely build output directories
        project_root = Path(__file__).parent / "src/S7Tools/bin"
        candidates = []
        for config in ["Debug", "Release"]:
            pattern = str(project_root / config / "net*")
            found = glob.glob(pattern)
            candidates.extend(found)
        if candidates:
            # Pick the most recently modified directory
            app_dir = Path(max(candidates, key=lambda d: os.path.getmtime(d)))
        else:
            raise FileNotFoundError("Could not find S7Tools build output directory. Please set S7TOOLS_BUILD_OUTPUT environment variable.")
    socat_profiles_dir = app_dir / "resources/SocatProfiles"
    profiles_file = socat_profiles_dir / "profiles.json"

    print(f"Looking for Socat profiles in: {socat_profiles_dir}")
    print(f"Profiles file should be at: {profiles_file}")
    print(f"Directory exists: {socat_profiles_dir.exists()}")
    print(f"Profiles file exists: {profiles_file.exists()}")

    if profiles_file.exists():
        try:
            with open(profiles_file, 'r') as f:
                data = json.load(f)
            print(f"Profiles file contains {len(data)} profiles")
            for profile in data:
                print(f"  - {profile.get('name', 'Unknown')} (ID: {profile.get('id', 'Unknown')})")
        except Exception as e:
            print(f"Error reading profiles file: {e}")
    else:
        print("No profiles.json file found - this is the issue!")

        # Create a simple test profiles.json to see if it helps
        test_profiles = [
            {
                "id": 1,
                "name": "Default",
                "description": "Default socat configuration for S7Tools",
                "isDefault": True,
                "isReadOnly": True,
                "createdAt": "2025-10-09T12:00:00Z",
                "modifiedAt": "2025-10-09T12:00:00Z",
                "configuration": {
                    "tcpPort": 1238,
                    "tcpHost": "localhost",
                    "verbose": True,
                    "hexDump": True,
                    "blockSize": 4,
                    "allowFork": True,
                    "reuseAddress": True,
                    "rawMode": True,
                    "noEcho": True
                }
            }
        ]

        try:
            socat_profiles_dir.mkdir(parents=True, exist_ok=True)
            with open(profiles_file, 'w') as f:
                json.dump(test_profiles, f, indent=2)
            print(f"Created test profiles.json file with {len(test_profiles)} profile(s)")
        except Exception as e:
            print(f"Error creating test profiles file: {e}")

if __name__ == "__main__":
    test_socat_service()
