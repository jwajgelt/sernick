import shutil
import random
import os

input_dir = r'Input'
expected_dir = r'Expected'
global_file_counter = 0
random.seed()

def remove_and_create_folders():
    for directory in [input_dir, expected_dir]:
        if os.path.exists(directory):
            shutil.rmtree(directory)
        os.makedirs(directory)


def create_files(num_files = 5, numbers_count=1000*1000, min_number = 1, max_number = 1000 * 1000 * 1000):
    global global_file_counter
    for file_idx in range(num_files):
        file_number = global_file_counter + file_idx
        global_file_counter += 1
        input_file = os.path.join(input_dir, str(file_number)+ '.in')
        output_file = os.path.join(expected_dir, str(file_number) + '.out')

        data=[random.randrange(min_number, max_number) for _ in range(numbers_count)]

        minimal = str(min(data))
        maximal = str(max(data))

        with open(input_file, 'a') as input:
            print(numbers_count, file=input)
            print(*data, file=input)
        
        with open(output_file, 'a') as output:
            print(minimal, file=output)
            print(maximal, file=output)     


def create_simple_tests():
    create_files(num_files=5, numbers_count=5, min_number=1, max_number=100)

def create_medium_tests():
    create_files(num_files=5, numbers_count=50, min_number=1, max_number=1000)

def create_large_tests():
    create_files(num_files=5, numbers_count=2000, min_number=1, max_number=1000 * 1000)

remove_and_create_folders()
create_simple_tests()
create_medium_tests()
create_large_tests()
