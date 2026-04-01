import os
import subprocess
import sys
import shutil

def run_command(command, cwd=None):
    print(f"Running: {' '.join(command)}")
    result = subprocess.run(command, cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Error: {result.stderr}")
        sys.exit(1)
    print(result.stdout)
    return result.stdout

def main():
    project_dir = "PicoBus.Core"
    dist_dir = "dist"
    
    # Clean and recreate dist directory
    if os.path.exists(dist_dir):
        shutil.rmtree(dist_dir)
    os.makedirs(dist_dir)

    print("--- Building and Packaging PicoBus ---")
    
    # 1. Clean
    run_command(["dotnet", "clean", "-c", "Release"], cwd=project_dir)
    
    # 2. Build
    run_command(["dotnet", "build", "-c", "Release"], cwd=project_dir)
    
    # 3. Pack
    run_command(["dotnet", "pack", "-c", "Release", "-o", os.path.abspath(dist_dir)], cwd=project_dir)

    print(f"--- Package prepared in {dist_dir}/ ---")

    # Optional Push
    if len(sys.argv) > 1 and sys.argv[1] == "push":
        api_key = os.environ.get("NUGET_API_KEY")
        if not api_key:
            print("Error: NUGET_API_KEY environment variable not set.")
            sys.exit(1)
        
        nupkgs = [f for f in os.listdir(dist_dir) if f.endswith(".nupkg")]
        if not nupkgs:
            print("No .nupkg found in dist folder.")
            sys.exit(1)
            
        # Sort and pick the latest if multiple exist (should only be one)
        latest_package = sorted(nupkgs)[-1]
        package_path = os.path.join(dist_dir, latest_package)
        
        print(f"--- Pushing {latest_package} to NuGet ---")
        run_command(["dotnet", "nuget", "push", package_path, "--api-key", api_key, "--source", "https://api.nuget.org/v3/index.json"])

if __name__ == "__main__":
    main()
