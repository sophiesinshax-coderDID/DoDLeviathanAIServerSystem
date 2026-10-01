import os
import sys
import ctypes

def is_admin():
    """Checks if the script is running with administrator privileges."""
    try:
        return ctypes.windll.shell32.IsUserAnAdmin()
    except:
        return False

def purge_background_tasks():
    # Add any other background executables that tend to duplicate here
    rogue_tasks = [
        "python.exe", 
        "node.exe", 
        "ollama.exe"
    ]
    
    print("==========================================")
    print("   AUTOMATED TASK PURGE INITIATED")
    print("==========================================")
    
    for task in rogue_tasks:
        # /f = force kill, /im = image name, /t = kill process tree (parents + children)
        print(f"Targeting: {task}...")
        exit_code = os.system(f"taskkill /f /im {task} /t >nul 2>&1")
        
        if exit_code == 0:
            print(f" [SUCCESS] Wiped all instances of {task}.")
        else:
            print(f" [INFO] No active instances found for {task}.")
            
    print("==========================================")
    print("All targeted background tasks cleared.")
    print("==========================================")
    input("Press Enter to close this window...")

if __name__ == "__main__":
    # Ensure it runs with admin rights so it can terminate stubborn tasks
    if not is_admin():
        print("Elevating privileges to terminate system processes...")
        ctypes.windll.shell32.ShellExecuteW(None, "runas", sys.executable, f'"{__file__}"', None, 1)
    else:
        purge_background_tasks()