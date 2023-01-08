import argparse
import os
import sys
import subprocess
import filecmp
from enum import Enum
import logging
from typing import List
from loglevel import LOG_LEVEL
from onlyForTestingTester import test_find_test_folders, test_get_compiled_files
from testHelpers import get_files, should_run_generator, create_output_expected_dirs, find_test_folders, compile_sernick_files, has_tests, clean_generated_files, INPUT_DIR, OUTPUT_DIR, EXPECTED_DIR, TEST_DIR_REGEX

# TODO refactor for more readable code
# TODO (bonus task?) generate report from all tests
# TODO refactor for less "os.path.join" -- maybe better structure would help?

class TestingLevel(Enum):
    ONLY_COMPILE=1
    COMPILE_AND_RUN_ON_INPUT=2

def prepare_parser():
    parser = argparse.ArgumentParser()
    parser.add_argument('--clean', action='store_true', help="Remove all generated Input/Output/Expected directories")
    parser.add_argument('--mockdata', action='store_true', help="Use binaries (prepared in advance) for Fibonacci just to test if Tester's logic works")
    parser.add_argument('--test_suite', help="Test suite to run")
    parser.add_argument('--loglevel', default='info',choices=logging._nameToLevel.keys(), help="Provide logging level. Example --loglevel debug'")
    parser.add_argument('--compiler', required=False, help="Path to compiler executable (default is src/sernick/bin/Debug/net6.0/sernick.dll)")
    return parser

def prepare_test_data(test_directory: str) -> TestingLevel:
    logging.info("Preparing test data for folder " + test_directory)

    create_output_expected_dirs(test_directory=test_directory)

    should_run_python_generator = should_run_generator(test_directory=test_directory)
    if should_run_python_generator:
        logging.debug("Running gen.py in " + test_directory + '...')
        subprocess.run([sys.executable, 'gen.py'], cwd=test_directory)
    else:
        logging.debug("No gen.py found, assuming Input/Expected folders are prepared...")

    if has_tests(test_directory=test_directory):
        return TestingLevel.COMPILE_AND_RUN_ON_INPUT
    else:
        return TestingLevel.ONLY_COMPILE

def compare_files(actual: str, expected:str)->None:
    are_equal = filecmp.cmp(actual, expected, shallow=False)
    if are_equal:
        logging.info("Correct answer on " + expected + " ! ✅")
    else:
        logging.info("Bad answer on " + expected + " ! ❌")
        sys.exit(1)

def run_file_on_input_and_check(binary_file_path: str, test_dir_path: str) -> None:
    logging.debug("Running a binary file {}".format(binary_file_path))

    input_dir_path = os.path.join(test_dir_path, INPUT_DIR)
    output_dir_path = os.path.join(test_dir_path, OUTPUT_DIR)
    expected_dir_path = os.path.join(test_dir_path, EXPECTED_DIR)

    for input_file_path in get_files(input_dir_path):
        input_file_basename_no_extension = os.path.splitext(os.path.basename(input_file_path))[0]
        output_file_path=os.path.join(output_dir_path, input_file_basename_no_extension) + '.out'
        expected_file_path=os.path.join(expected_dir_path, input_file_basename_no_extension) + '.out'

        input_fd = open(input_file_path, 'r')
        output_fd = open(output_file_path, 'w')

        p = subprocess.Popen([binary_file_path], stdin=input_fd, text=True,stdout=output_fd)
        p.wait()
        compare_files(actual=output_file_path, expected=expected_file_path)

def run_files(compiled_files: List[str], test_directory: str)->None:
    for binary_file in compiled_files:
        try:
            run_file_on_input_and_check(binary_file_path=binary_file, test_dir_path=test_directory)
        except Exception as e:
            logging.error("Exception occurred when running {}, proceeding...".format(binary_file), exc_info=e)

def test(use_mock_data: bool, compiler_path: str = None, test_directories: List[str] = None):
    if test_directories is None:
        test_directories = test_find_test_folders() if use_mock_data else list(find_test_folders('.'))

    for test_directory in test_directories:
        logging.info("-----------")
        logging.info("Entering {}...".format(test_directory))
        testing_level = prepare_test_data(test_directory)

        if use_mock_data:
            compiled_files = test_get_compiled_files(test_directory=test_directory) # just for testing TODO uncomment for the line below
        else:
            compiled_files = compile_sernick_files(test_directory, compiler_path) 

        if testing_level == TestingLevel.ONLY_COMPILE:
            logging.info("Compilation executed, not running further (no test input)")
        elif testing_level == TestingLevel.COMPILE_AND_RUN_ON_INPUT:
            run_files(compiled_files=compiled_files, test_directory=test_directory)

def clean():
    for test_directory in test_find_test_folders():
        logging.info("Cleaning {}".format(test_directory))
        clean_generated_files(test_directory=test_directory)
        

def run():
    parser = prepare_parser()
    args = parser.parse_args()
    logging.basicConfig(level=args.loglevel.upper())

    if args.clean:
        clean()
        return
    if args.mockdata:
        test(use_mock_data=True)
    else:
        test(use_mock_data=False, compiler_path=args.compiler, test_directories=[args.test_suite] if args.test_suite else None)

if __name__ == '__main__':
    run()

