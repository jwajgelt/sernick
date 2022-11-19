### Sernick

### Types:

`Bool, Int, Unit`

### Entry point

Entire program is executed as a script. There is no `Int main()`

### How to name types/variables/functions

Types are syntactically different from functions and variables. Type names begin with Uppercase Latin characters, functions and variable names begin from lowercase. Only alphanumeric characters are permitted in identifiers.

Variables are declared by `const/var variableName: TypeName

Adding a type is optional, as far as the variable is immediately initialized.

Line separator is `;`

### Functions

- Functions are declared using `fun` keyword
- All arguments are readonly
- Every function declares its return type e.g. `fun foo(): ReturnType {...}`
  * If the there is no explicitly declared return type, then the function has to return `Unit`, so:\
    `fun foo() {...}` is equivalent to `fun foo(): Unit {...}`
- Every argument should declare its type (even if it has a default value)
- It is possible to have a default value for a function's argument, however:
  - Only const expr are permitted (no referencing other variables!)
  - Set of arguments with a default value must be a suffix of function declaration (in other words, this is incorrect: `fun bar(a: Int = 1, b, c:Int=4`)

### Control flow

We can use `if/else` in Sernick

Full syntax looks like this:

```
if(condition){ ... } else { ...}
...

// else branch is optional, could be just true branch
if(condition){ ... }
...


```

### Loops

We currently have only `loop` instruction which behaves like `while(true)` loop
Inside the `loop`, there has to be `break` or `return`, otherwise the program is not syntactically correct

**Example:**

```
var x: Int = 0;
loop {
 x = x + 1;
 if(x == 10){ break; }
}
```

We also have a `continue` instruction;

### Comments

We use `// one-line comment` for one-line comments and

```
/**
* Multi-line
* comment syntax from C
*/
```

for multi-line comments

### Blocks of code

We use curly braces `{,}` to define new blocks of code
You could also use `()` to group code, however, only `{}` introduces a new scope
