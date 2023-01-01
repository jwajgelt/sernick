import random
import os
import subprocess
import re
import filecmp
from enum import Enum
from typing import List
from testHelpers import get_files, find_test_folders, get_compiled_files, has_tests, INPUT_DIR, OUTPUT_DIR, EXPECTED_DIR, TEST_DIR_REGEX

# TODO refactor for more readable code
# TODO refactor for less "os.path.join" -- maybe better structure would help?
# TODO (bonus task) add argparse to do something like "python3 tester.py --clean" which would clean Output/Input/Expected directories
# TODO (bonus task): rewrite print -> Logger

class TestingLevel(Enum):
    ONLY_COMPILE=1
    COMPILE_AND_RUN=2


def prepare_test_data(test_directory: str) -> TestingLevel:
    print("Preparing test data for folder " + test_directory)
    expected_dir_path = os.path.join(test_directory, EXPECTED_DIR)
    if not os.path.exists(expected_dir_path):
        os.makedirs(expected_dir_path)

    all_files_in_dir = get_files(test_directory)
    directory_contains_python_generator = True if 'gen.py' in all_files_in_dir else False
    if directory_contains_python_generator:
        print("Running gen.py in " + test_directory)
        subprocess.run(['/usr/bin/python3', 'gen.py'], cwd=test_directory)

    if has_tests(test_directory=test_directory)
        return TestingLevel.COMPILE_AND_RUN
    else:
        return TestingLevel.ONLY_COMPILE


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

def __TEST_GET_COMPILED_FILES(test_directory: str) -> List[str]:
    return [
            os.path.join(test_directory, "fibonacci-in-c"),
            os.path.join(test_directory, "fibonacci-in-c-2")
        ]

def __TEST_FIND_TEST_FOLDERS() -> str:
    return [os.path.join('.', 'FibonacciTest')]



def run_files(compiled_files: List[str], test_directory: str)->None:
    for binary_file in compiled_files:
        try:
            run_file(binary_file_path=binary_file, test_directory=test_directory)
            check_output(test_directory)
        except Exception:
            pass
    
def test():
    for test_directory in find_test_folders('.'):
        print("-----------")
        
        testing_level = prepare_test_data(test_directory)

        compiled_files = __TEST_GET_COMPILED_FILES(test_directory=test_directory) # just for testing TODO uncomment for the line below
        # compiled_files = compile_sernick_files(directory) 

        if testing_level == TestingLevel.ONLY_COMPILE:
            print("Compilation successful, not running (no test input)")
        elif testing_level == TestingLevel.COMPILE_AND_RUN:
            run_files(compiled_files=compiled_files, test_directory=test_directory)

if __name__ == '__main__':
    test()