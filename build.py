import os
import shutil
import subprocess
import zipfile

def build():
    print("Building Fluent Scrcpy...")
    
    # Run PyInstaller
    subprocess.check_call(['pyinstaller', 'FluentScrcpy.spec', '--noconfirm', '--clean'])
    
    # Zip the output
    dist_dir = os.path.join('dist', 'FluentScrcpy')
    zip_name = 'FluentScrcpy-v1.0.zip'
    
    print(f"Zipping {dist_dir} to {zip_name}...")
    
    with zipfile.ZipFile(zip_name, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(dist_dir):
            for file in files:
                file_path = os.path.join(root, file)
                # Archive name should be relative to dist/FluentScrcpy
                arcname = os.path.relpath(file_path, 'dist')
                zipf.write(file_path, arcname)
                
    print(f"Build complete! Release package: {os.path.abspath(zip_name)}")

if __name__ == '__main__':
    build()
