import os
import subprocess
import re
import filecmp
from typing import List


INPUT_DIR = r'Input'
OUTPUT_DIR = r'Output'
EXPECTED_DIR = r'Expected'
TEST_DIR_REGEX = re.compile('.*Test')

def find_test_folders(root_dir: str):
    for dirpath, subdirs, _ in os.walk(root_dir):
        for dir in subdirs:
            if TEST_DIR_REGEX.match(dir):
                yield os.path.join(dirpath, dir)

# Credits @Acorn in https://stackoverflow.com/a/5900590/13942930
def is_sernick_file(file_path: str) -> bool:
    if not os.path.isfile(file_path):
        return False
    # Split the extension from the path and normalize it to lowercase.
    ext = os.path.splitext(file_path)[-1].lower()
    return ext == 'ser'

def get_compiled_files(dir)-> List[str]:    
    sernick_files = [f for f in os.listdir(dir) if is_sernick_file(os.path.join(dir, f))]
    compiled_files = []
    for f in sernick_files:
        pass # TODO call subprocess with something like nasm
    print('Compiled the following files: {}'.format(compiled_files))
    return compiled_files

def get_files(directory: str) -> List[str]:
    return [os.path.join(directory, f) for f in os.listdir(directory) if os.path.isfile(os.path.join(directory, f))]

def prepare_test_data(test_directory: str) -> bool:
    print("Preparing test data for folder " + test_directory)
    all_files_in_dir = get_files(test_directory)
    directory_contains_python_generator = True if 'gen.py' in all_files_in_dir else False
    if directory_contains_python_generator:
        print("Running gen.py in " + test_directory)
        subprocess.run(['/usr/bin/python3', 'gen.py'], cwd=test_directory)
    else:
        print("No gen.py, assuming tests are already there...")
    expected_dir_path = os.path.join(test_directory, EXPECTED_DIR)
    if not os.path.exists(expected_dir_path):
        os.makedirs(expected_dir_path)

def run_file(binary_file_path: str, test_directory: str) -> None:
    print("Running {}".format(binary_file_path))

    input_dir = os.path.join(test_directory, INPUT_DIR)
    for input_file in get_files(input_dir):
        output_file_name=os.path.join(test_directory, OUTPUT_DIR, os.path.splitext(os.path.basename(input_file))[0])+'.out'
        input_fd = open(input_file, 'r')
        output_fd = open(output_file_name, 'w')
        p = subprocess.Popen([binary_file_path], stdin=input_fd, text=True,stdout=output_fd)
        p.wait()

def check_output(test_directory: str) -> None:
    output_dir = os.path.join(test_directory, OUTPUT_DIR)
    expected_dir = os.path.join(test_directory, EXPECTED_DIR)
    
    output_files = get_files(output_dir)
    expected_files = get_files(expected_dir)

    for actual, expected in zip(output_files, expected_files):
        files_equal = filecmp.cmp(actual, expected, shallow=False)
        if files_equal:
            print("Correct answer on " + expected + " ! ✅")
        else:
            print("Bad answer on " + expected + " ! ❌")

def has_tests(test_directory: str)->bool:
    input_dir = os.path.join(test_directory, INPUT_DIR)
    output_dir = os.path.join(test_directory, OUTPUT_DIR)
    expected_dir = os.path.join(test_directory, EXPECTED_DIR)
    for d in [input_dir, output_dir, expected_dir]:
        if not os.path.exists(d):
            print("No directory named {} for {}".format(d, test_directory))
            return False
    return True