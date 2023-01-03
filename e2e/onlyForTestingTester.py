import os
from typing import List

def test_get_compiled_files(test_directory: str) -> List[str]:
    return [
            os.path.join(test_directory, "fibonacci-in-c"),
            os.path.join(test_directory, "fibonacci-in-c-2")
        ]

def test_find_test_folders() -> str:
    return [os.path.join('.', 'FibonacciTest')]
