import shutil
import random
import os

input_dir = r'Input'
expected_dir = r'Expected'
global_file_counter = 0
random.seed(0)

def is_prime(n):
    if n == 1:
        return False
    if n == 2 or n == 3:
        return True
    i=2
    while i*i <= n:
        if n % i == 0:
            return False
        i =  i + 1
    return True

def remove_and_create_folders():
    for directory in [input_dir, expected_dir]:
        if os.path.exists(directory):
            shutil.rmtree(directory)
        os.makedirs(directory)

def create_file(n=None):
    global global_file_counter

    if n is None:
        n = random.randrange(1, 5000)

    file_number = global_file_counter
    global_file_counter += 1
    input_file = os.path.join(input_dir, str(file_number)+ '.in')
    output_file = os.path.join(expected_dir, str(file_number) + '.out')

    with open(input_file, 'a') as input:
        print(n, file=input)
    
    with open(output_file, 'a') as output:
        print(int(is_prime(n)), file=output)

def create_files(num_files = 10):
    for _ in range(num_files):
        create_file(n=None)


def create_simple_tests():
    create_file(n=1)
    create_file(n=2)
    create_file(n=3)
    create_file(n=5)
    create_file(n=7)
    create_file(n=10)

def create_medium_tests():
    create_files(num_files=10)

remove_and_create_folders()
create_simple_tests()
create_medium_tests()
