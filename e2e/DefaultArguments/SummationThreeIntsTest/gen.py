import shutil
import random
import os

input_dir = r'Input'
expected_dir = r'Expected'
global_file_counter = 0
random.seed(0)

def sum_of_three(a=1, b=2, c=3):
    return a + b + c

def remove_and_create_folders():
    for directory in [input_dir, expected_dir]:
        if os.path.exists(directory):
            shutil.rmtree(directory)
        os.makedirs(directory)

def print_with_newlines(values, file):
    for value in values:
        print(value, file=file)

def create_file():
    global global_file_counter

    a = random.randrange(1, 100)
    b = random.randrange(1, 100)
    c = random.randrange(1, 100)

    file_number = global_file_counter
    global_file_counter += 1
    input_file = os.path.join(input_dir, str(file_number)+ '.in')
    output_file = os.path.join(expected_dir, str(file_number) + '.out')

    with open(input_file, 'w') as input:
        print_with_newlines(values= [a,b,c], file=input)
    
    with open(output_file, 'w') as output:
        print_with_newlines(values=[
            sum_of_three(),
            sum_of_three(a=a),
            sum_of_three(a=a, b=b),
            sum_of_three(a=a, b=b, c=c)
        ], file=output)

def create_files(num_files = 10):
    for _ in range(num_files):
        create_file()

def create_tests():
    create_files(num_files=10)

remove_and_create_folders()
create_tests()
