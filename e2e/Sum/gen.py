import random

n = 1000
random.seed()

print(n)

data=[random.randrange(1, 1000) for i in range(n)]

print(*data)
print(sum(data))
