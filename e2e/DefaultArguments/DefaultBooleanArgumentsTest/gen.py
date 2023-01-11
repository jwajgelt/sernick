import shutil
import random
import os

input_dir = r'Input'
expected_dir = r'Expected'
global_file_counter = 0
random.seed(0)

def boolean_and(a=True, b=False):
    return a and b

def boolean_or(a=False, b=True):
    return a or b

def remove_and_create_folders():
    for directory in [input_dir, expected_dir]:
        if os.path.exists(directory):
            shutil.rmtree(directory)
        os.makedirs(directory)

def print_with_newlines(values, file):
    for value in values:
        print(int(value), file=file)

def create_file():
    global global_file_counter

    a = random.getrandbits(1)
    b = random.getrandbits(1)

    file_number = global_file_counter
    global_file_counter += 1
    input_file = os.path.join(input_dir, str(file_number)+ '.in')
    output_file = os.path.join(expected_dir, str(file_number) + '.out')

    with open(input_file, 'w') as input:
        print_with_newlines(values= [a,b], file=input)
    
    with open(output_file, 'w') as output:
        print_with_newlines(values=[
            boolean_and(),
            boolean_and(a=a),
            boolean_and(a=a,b=b),
            boolean_or(),
            boolean_or(a=a),
            boolean_or(a=a, b=b)
        ], file=output)

def create_files(num_files = 5):
    for _ in range(num_files):
        create_file(n=None)

def create_tests():
    create_files(num_files=5)

create_tests()
