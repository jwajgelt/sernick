import shutil
import random
import os
import subprocess
import re
from typing import List

input_dir = r'Input'
output_dir = r'Output'
global_file_counter = 0
random.seed()

def find_test_folders():
    test_folder_pattern = re.compile('.*Test')
    for _, dirs, _ in os.walk("."):
        for dir in dirs:
            if test_folder_pattern.match(dir):
                yield dir

# Credits @Acorn in https://stackoverflow.com/a/5900590/13942930
def is_sernick_file(file_path: str) -> bool:
    if not os.isfile(file_path):
        return False
    # Split the extension from the path and normalize it to lowercase.
    ext = os.path.splitext(file_path)[-1].lower()
    return ext == 'ser'

def compile_sernick_files(dir)-> List[str]:    
    sernick_files = [f for f in os.listdir(dir) if is_sernick_file(os.join(dir, f))]
    for f in sernick_files:
        pass # TODO call subprocess with something like nasm
    return [f[:-3] for f in sernick_files]

def prepare_input_output(directory: str) -> None:
    files = [f for f in os.listdir(directory) if os.isfile(os.join(directory, f))]
    directory_contains_genpy = True if 'gen.py' in files else False
    if directory_contains_genpy:
        subprocess.call('python3', 'gen.py')

def test_file(binary_file_path: str) -> None:
    for _, _, files in os.walk(os.path.join(".", "Input")):
        for input_file_path in files:
            subprocess.Popen(binary_file_path, stdin=input_file_path, text=True)

def test():
    for directory in find_test_folders():
        prepare_input_output(directory)
        compiled_files = compile_sernick_files(directory)
        for binary_file in compiled_files:
            test_file(binary_file_path=binary_file)

if __name__ == '__main__':
    test()