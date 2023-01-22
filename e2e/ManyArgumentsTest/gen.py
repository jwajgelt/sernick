import shutil
import random
import os

input_dir = r'Input'
expected_dir = r'Expected'
global_file_counter = 0
random.seed(0)

def calculate_output(x1, x2, x3, x4, x5):
    return f"{x5}\n{x1 + x2 + x3 + x4 + x5}"

def remove_and_create_folders():
    for directory in [input_dir, expected_dir]:
        if os.path.exists(directory):
            shutil.rmtree(directory)
        os.makedirs(directory)

def create_file(x1, x2, x3, x4, x5):
    global global_file_counter

    t = [x1, x2, x3, x4, x5]

    file_number = global_file_counter
    global_file_counter += 1
    input_file = os.path.join(input_dir, str(file_number)+ '.in')
    output_file = os.path.join(expected_dir, str(file_number) + '.out')

    with open(input_file, 'a') as input:
        for i in t:
            print(i, file=input)
    
    with open(output_file, 'a') as output:
        print(calculate_output(x1, x2, x3, x4, x5), file=output)

def create_files(num_files = 10):
    for _ in range(num_files):
        randomList = []
        for _ in range(5):
            randomList.append(random.randint(0, 500))
        (x1, x2, x3, x4, x5) = randomList
        create_file(x1, x2, x3, x4, x5)

def create_simple_tests():
    create_file(1, 2, 3, 4, 5)
    create_file(100, 110, 120, 150, 200)

def create_medium_tests():
    create_files(num_files=10)

remove_and_create_folders()
create_simple_tests()
create_medium_tests()
