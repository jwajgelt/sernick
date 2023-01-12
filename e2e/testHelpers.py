import os
import sys
import subprocess
import shutil
import re
import filecmp
from typing import List
import logging

INPUT_DIR = r'Input'
OUTPUT_DIR = r'Output'
EXPECTED_DIR = r'Expected'
TEST_DIR_REGEX = re.compile('.*Test')
SERNICK_EXE_PATH = os.path.join('..', 'src', 'sernick', 'bin','Debug', 'net6.0', 'sernick.dll')

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
    return ext == '.ser'

def drop_extension(path: str)->str:
    return os.path.splitext(path)[0]

def join_path(dir: str, file_path: str)->str:
    return os.path.join(dir,file_path)

def compile_sernick_files(dir: str, compiler_path: str = None)-> List[str]:    
    sernick_files = [join_path(dir, f) for f in os.listdir(dir) if is_sernick_file(join_path(dir,f))]
    logging.debug("Found following sernick files: {}".format(sernick_files))
    
    compiled_files = []

    for file_path in sernick_files:
        try:
            completed_process = subprocess.run(["dotnet", compiler_path or SERNICK_EXE_PATH, file_path])
            completed_process.check_returncode() # this raises an exception
            compiled_files.append(drop_extension(file_path) + ".out")
        except Exception as e:
            logging.error("Could not compile {} ❌".format(file_path), exc_info=e)
            raise e

    logging.info('Compiled the following files: {}'.format(compiled_files))
    return compiled_files

def get_files(directory: str) -> List[str]:
    return [os.path.join(directory, f) for f in os.listdir(directory) if os.path.isfile(os.path.join(directory, f))]

def prepare_test_data(test_directory: str) -> bool:
    logging.info("Preparing test data for folder " + test_directory)
    all_files_in_dir = get_files(test_directory)
    directory_contains_python_generator = True if 'gen.py' in all_files_in_dir else False
    if directory_contains_python_generator:
        logging.debug("Running gen.py in " + test_directory)
        subprocess.run([sys.executable, 'gen.py'], cwd=test_directory)
    else:
        logging.debug("No gen.py, assuming tests are already there...")
    expected_dir_path = os.path.join(test_directory, EXPECTED_DIR)
    if not os.path.exists(expected_dir_path):
        os.makedirs(expected_dir_path)

def run_file(binary_file_path: str, test_directory: str) -> None:
    logging.debug("Running {}".format(binary_file_path))

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
            logging.info("Correct answer on " + expected + " ! ✅")
        else:
            logging.info("Bad answer on " + expected + " ! ❌")

def has_tests(test_directory: str)->bool:
    input_dir = os.path.join(test_directory, INPUT_DIR)
    output_dir = os.path.join(test_directory, OUTPUT_DIR)
    expected_dir = os.path.join(test_directory, EXPECTED_DIR)

    for d in [input_dir, output_dir, expected_dir]:
        if not os.path.exists(d):
            logging.debug("No directory named {} for {}".format(d, test_directory))
            return False
    return True

def clean_generated_files(test_directory: str)->bool:
    input_dir = os.path.join(test_directory, INPUT_DIR)
    output_dir = os.path.join(test_directory, OUTPUT_DIR)
    expected_dir = os.path.join(test_directory, EXPECTED_DIR)

    for d in [input_dir, output_dir, expected_dir]:
        if os.path.exists(d):
            shutil.rmtree(d)

def should_run_generator(test_directory: str)->bool:
    all_files_in_dir = get_files(test_directory)
    basenames = [os.path.basename(f) for f in all_files_in_dir]
    return 'gen.py' in basenames

def create_output_expected_dirs(test_directory: str):
    expected_dir_path = os.path.join(test_directory, EXPECTED_DIR)
    output_dir_path = os.path.join(test_directory, OUTPUT_DIR)
    for d in [expected_dir_path, output_dir_path]:
        if not os.path.exists(d):
            os.makedirs(d)
