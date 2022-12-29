import shutil
import random
import os

input_dir = r'Input'
output_dir = r'Output'
global_file_counter = 0
random.seed()

# Copyright https://realpython.com/fibonacci-sequence-python/
def fibonacci_of(n):
    # Validate the value of n
    if not (isinstance(n, int) and n >= 0):
        raise ValueError(f'Positive integer number expected, got "{n}"')

    # Handle the base cases
    if n in {0, 1}:
        return n

    previous, fib_number = 0, 1
    for _ in range(2, n + 1):
        # Compute the next Fibonacci number, remember the previous one
        previous, fib_number = fib_number, previous + fib_number

    return fib_number


def remove_and_create_folders():
    for directory in [input_dir, output_dir]:
        shutil.rmtree(directory)
        os.makedirs(directory)


def create_file(n=None):
    global global_file_counter

    if n is None:
        n = random.randrange(0, 45)

    file_number = global_file_counter
    global_file_counter += 1
    input_file = os.path.join(input_dir, str(file_number)+ '.in')
    output_file = os.path.join(output_dir, str(file_number) + '.out')

    with open(input_file, 'a') as input:
        print(n, file=input)
    
    with open(output_file, 'a') as output:
        print(fibonacci_of(n), file=output)

def create_files(num_files = 10):
    for _ in range(num_files):
        create_file(n=None)


def create_simple_tests():
    create_file(n=1)
    create_file(n=2)
    create_file(n=3)
    create_file(n=4)
    create_file(n=10)

def create_other_tests():
    create_files(num_files=10)

remove_and_create_folders()
create_simple_tests()
create_other_tests()
