import random

n = 1000 * 1000
random.seed()

print(n)

data=[random.randrange(1, 1000*1000*1000) for i in range(n)]

minimal = str(min(data))
maximal = str(max(data))

print(*data)
print(' '.join([minimal, maximal]))
