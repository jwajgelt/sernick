import random

n = 100 * 1000
random.seed()

print(n)

data=[str(random.randrange(1, 1000*1000*1000)) for i in range(n)]

minimal = str(min(data))
maximal = str(max(data))

print(' '.join(data))
print(' '.join([minimal, maximal]))
