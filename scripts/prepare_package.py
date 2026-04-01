import os
import subprocess
import sys
import shutil
import re
import argparse

def run_command(command, cwd=None):
    print(f"Running: {' '.join(command)}")
    result = subprocess.run(command, cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Error: {result.stderr}")
        sys.exit(1)
    print(result.stdout)
    return result.stdout

def increment_version(part):
    project_file = "PicoBus.Core/PicoBus.Core.csproj"
    with open(project_file, 'r', encoding='utf-8') as f:
        content = f.read()

    version_match = re.search(r'<Version>(.*?)</Version>', content)
    if not version_match:
        print("Error: Could not find <Version> in csproj.")
        sys.exit(1)

    current_version = version_match.group(1)
    parts = current_version.split('.')
    while len(parts) < 3:
        parts.append('0')

    major, minor, patch = int(parts[0]), int(parts[1]), int(parts[2])

    if part == 'major':
        major += 1
        minor = 0
        patch = 0
    elif part == 'minor':
        minor += 1
        patch = 0
    elif part == 'patch':
        patch += 1

    new_version = f"{major}.{minor}.{patch}"
    print(f"--- Incrementing version: {current_version} -> {new_version} ---")
    
    new_content = re.sub(r'<Version>.*?</Version>', f'<Version>{new_version}</Version>', content)
    with open(project_file, 'w', encoding='utf-8') as f:
        f.write(new_content)
    
    return new_version

def main():
    parser = argparse.ArgumentParser(description="Prepare and optionally push PicoBus NuGet package.")
    parser.add_argument('--version', choices=['major', 'minor', 'patch'], help="Part of the version to increment.")
    parser.add_argument('--push', action='store_true', help="Push the generated package to NuGet.")
    args = parser.parse_args()

    project_dir = "PicoBus.Core"
    dist_dir = "dist"
    
    # 1. Handle version incrementing
    if args.version:
        increment_version(args.version)

    # Clean and recreate dist directory
    if os.path.exists(dist_dir):
        shutil.rmtree(dist_dir)
    os.makedirs(dist_dir)

    print("--- Building and Packaging PicoBus ---")
    
    # 2. Clean
    run_command(["dotnet", "clean", "-c", "Release"], cwd=project_dir)
    
    # 3. Build
    run_command(["dotnet", "build", "-c", "Release"], cwd=project_dir)
    
    # 4. Pack
    run_command(["dotnet", "pack", "-c", "Release", "-o", os.path.abspath(dist_dir)], cwd=project_dir)

    print(f"--- Package prepared in {dist_dir}/ ---")

    # 5. Optional Push
    if args.push:
        api_key = os.environ.get("NUGET_API_KEY")
        if not api_key:
            print("Error: NUGET_API_KEY environment variable not set.")
            sys.exit(1)
        
        nupkgs = [f for f in os.listdir(dist_dir) if f.endswith(".nupkg")]
        if not nupkgs:
            print("No .nupkg found in dist folder.")
            sys.exit(1)
            
        latest_package = sorted(nupkgs)[-1]
        package_path = os.path.join(dist_dir, latest_package)
        
        print(f"--- Pushing {latest_package} to NuGet ---")
        run_command(["dotnet", "nuget", "push", package_path, "--api-key", api_key, "--source", "https://api.nuget.org/v3/index.json"])

if __name__ == "__main__":
    main()
