# Vext Programming Language

Created by [Guy Slutzker](https://github.com/Guy1414)

[![Current Version](https://img.shields.io/github/v/release/Guy1414/Vext?include_prereleases&color=28A745&label=Version)](https://github.com/Guy1414/Vext/releases/latest)

---

📚 Documentation

Vext has two dedicated documentation repositories:

📘 Language Documentation
Learn the syntax, features, and usage of Vext:
[GitHub Link](https://github.com/Guy1414/Vext-Language-Documentation)

🛠 Codebase Documentation
Explore the internal architecture, modules, and implementation details:
[GitHub Link](https://github.com/Guy1414/Vext-Codebase-Documentation)

---

## 📄 License

Vext is open source and licensed under the **MIT License**.

Copyright (c) 2026 Guy Slutzker

You are free to use, modify, and distribute this software under the terms of the license.

See the [LICENSE](LICENSE) file for full details.

---

## 🤝 Contributing

Contributions, suggestions, and improvements are welcome.

Please read the [CONTRIBUTING.md](CONTRIBUTING.md) file before submitting issues or pull requests.  
Explore the internal architecture, modules, and implementation details:
[GitHub link](https://github.com/Guy1414/Vext-Codebase-Documentation)

---

## 🚀 How To Run

1. **Download the latest version:**
   [![Download Latest EXE](https://img.shields.io/badge/Download%20Latest%20.exe-28A745)](https://github.com/Guy1414/Vext/releases/latest)  
   *(Download the `.exe` file from the **Assets** section at the bottom of the page)*

2. **Launch:** Run the downloaded `.exe` file.
3. **Edit Code:** Press **Enter** to open a Notepad window. Edit the code as you like, then **Save and Close** the Notepad window.
4. **Load:** The application will automatically ingest the code you just saved and display it.
5. **Compile:** Press **Enter** to compile the code.
6. **Execute:** Press **Enter** again to execute.
7. **Done!**

> [!TIP]
> If you see a "Windows protected your PC" popup, click **More info** and then **Run anyway**.

---

## Activity

![Alt](https://repobeats.axiom.co/api/embed/b5869f1ccef5f3c30ad8f33cabf770ce3aa69a05.svg "Repobeats analytics image")

---

## Overview

Vext is a programming language designed for performance, simplicity, and expressive syntax. It features full expressions, deep function calls, constant folding, and a custom virtual machine for execution.

---

### [The Trello page with the planned features and thoughts](https://trello.com/b/GVN18N7R/vext).

## Features

### Core Language
- **Variables:** Declaration, use, type checking, `auto` type inference  
- **Types:** `int`, `float`, `bool`, `string`, `auto`  
- **Expressions:** Nested arithmetic, boolean logic, comparisons, unary operators, function calls, mixed-type math  
- **Operators:**  
  - Arithmetic: `+ - * / % **`  
  - Comparison: `== != < > <= >=`  
  - Logic: `&& || !`  
  - Unary: `++ -- -`  
  - Assignment / Compound: `= += -= *=`  
  - String concatenation: `+` (works with numbers and booleans)  
- **Strings:** Escape sequences (`\n`, `\t`, `\"`, `\'`, `\\`), automatic conversion when concatenating other types  

### Control Flow
- `if / else if / else`  
- `while` loops  
- `for` loops  
- Nested loops and optimized loops for constant increments  

### Functions
- **Typed Parameters:** Explicit types or `auto` for flexible inputs  
- **Return Types:** Typed return values or `void`  
- **Overloading:** Support for multiple functions with the same name but different parameter counts  
- **Default Values:** Parameters can have optional default initializers  
- **Nested Calls:** Functions can be passed as arguments to other functions  

### Constant Folding & Compile-Time Optimization
- Nested expressions evaluated at compile time  
- Binary and unary operations folded  
- Boolean short-circuiting handled  
- Strings and numeric types are automatically folded  

### Standard Library & Intrinsics

#### IO Module (`IO.`)
The IO module is loaded only when used, keeping the runtime more lightweight.

##### Input
- `IO.ReadLine()` - string
- `IO.ReadInt()` - int
- `IO.ReadFloat()` - float

##### Output
- `IO.Print(value)` - prints without newline
- `IO.Print()` - prints empty string
- `IO.Println(value)` - prints with newline
- `IO.Println()` - prints newline

#### Math Module (`Math.`)
- `Math.Pow(num, power)`
- `Math.Sqrt(num)`
- `Math.Sin(num)`
- `Math.Cos(num)`
- `Math.Tan(num)`
- `Math.Log(num)`
- `Math.Exp(num)`
- `Math.Random()` / `Math.Random(min, max)`
- `Math.Abs(num)`
- `Math.Round(num)`
- `Math.Floor(num)`
- `Math.Ceil(num)`
- `Math.Min(a, b)`
- `Math.Max(a, b)`

#### Intrinsic Members
Works on any variable using the `.` operator:
- `.Type`: Returns the type name as a string (e.g., `"int"`, `"string"`).
- `.ToString()`: Returns the string representation of the value.
- `.Length`: (Strings only) Returns the length of the string.

### Editor Support (LSP)
The Vext project includes a VSCode extension and a Language Server Protocol (LSP) implementation:
- **Syntax Highlighting:** Full TextMate grammar for `.vext` and `.vxt` files.
- **Semantic Highlighting:** Advanced highlighting for functions, variables, and keywords based on compiler analysis.
- **Diagnostics:** Real-time error reporting and semantic validation.
- **Commands:** Integrated "Run Vext Code" command via the editor UI.

### Compiler Architecture
Vext features a full compilation pipeline:
1. **Lexer:** Tokenizes source code  
2. **Parser:** Builds an abstract syntax tree (AST)  
3. **Semantic Pass:** Type checking, variable resolution, constant folding, and semantic token generation  
4. **Bytecode Generator:** Converts AST into Vext bytecode  
5. **VextVM:** High-performance stack-based virtual machine  

---

## Abstract Syntax Tree (AST) Node Types

**Expressions:**
- `BinaryExpressionNode` (`+`, `-`, `*`, `/`, `**`, `%`, etc.)  
- `UnaryExpressionNode` (`++`, `--`, `-`, `!`)  
- `LiteralNode` (numbers, strings, booleans, null)  
- `VariableNode`  
- `FunctionCallNode`  
- `MemberAccessNode` (Covers `Module.Func`, `.Type`, `.ToString()`, `.Length`)  

**Statements:**
- `VariableDeclarationNode`  
- `IfStatementNode`  
- `WhileStatementNode`  
- `ForStatementNode`  
- `ReturnStatementNode`  
- `AssignmentStatementNode` (`=`, `+=`, `-=`, `*=`)
- `IncrementStatementNode`  
- `FunctionDefinitionNode`  

---

# **Example Program:**
```
// --- 1. Basic Types & Declarations ---
int i = 42;
float f = 3.14159;
bool flag = true;
string text = "Hello, World!";
auto inferredInt = 100;
auto inferredFloat = 0.25;
auto inferredBool = false;
auto inferredString = "auto text";

// --- 2. Arithmetic & Type Coercion ---
int sum = i + 10;
float result = f * 2 - 1.5;
string concat = text + " " + inferredString + " " + sum + " " + result;
bool complexBool = (i > 10 && f < 10.0) || !flag;

// --- Intrinsic Members ---
IO.Println("Type of i: " + i.Type);            // "int"
IO.Println("String of f: " + f.ToString());    // "3.14159"
IO.Println("Length: " + text.Length);        // 13

// --- 3. Functions & Overloading ---
int square(int n) { return n * n; }
string greet(string name = "Guy") { return "Hello, " + name + "!"; }

IO.Println(greet("Vext")); // "Hello, Vext!"
IO.Println(greet());        // "Hello, Guy!"

// --- 3. Unary & Compound Operators ---
i++;
sum += 5;
result *= 2.0;
concat += "!";
bool testNegation = !complexBool;

// --- 4. Strings & Escapes ---
string escaped = "Line1\\nLine2\\tTabbed\\\"Quote\\'Single";
IO.Println(escaped);

// --- 5. Comments ---
IO.Println("Comments ignored");

// --- 6. Conditionals ---
if (i > 40) {
    IO.Println("i > 40");
} else if (i == 42) {
    IO.Println("i == 42");
} else {
    IO.Println("i < 40");
}

// --- 7. Loops ---
int total = 0;
for (int j = 0; j < 5; j++) {
    total += j;
    if (j % 2 == 0) IO.Println("Even: " + j);
}
int k = 0;
while (k < 3) {
    IO.Println("While: " + k);
    k++;
}

// --- 8. Functions & Nested Calls ---
float multiply(float a, float b) { return a * b; }
int addThree(auto a, auto b, auto c) { return a + b + c; } // auto allows int/float mix

int sq = square(3);
int val = addThree(1, 2, sq);
float calc = multiply(2.5, square(4));
string message = greet("Vext");

// --- 9. Nested Expressions & ConsTant Folding ---
float complexCalc = ((2 + 3) * (5 - 1) / 2) + Math.Pow(2, 3) - 4;
int nestedFold = square(addThree(1, 2, 3)) + square(2);

// --- 10. Booleans & Logic ---
bool logicTest = (true && false) || (false || true) && !false;

// --- 11. Advanced Operators ---
int a = 10;
int b = 3;
int mod = a % b;
float exp = Math.Pow(a, b); // 10^3

for (int j = 0; j < 3; j++) {
    IO.Println("Loop: " + j);
}

// --- 14. Math & Trigonometry ---
float angle = 0.5;
float trigTest = Math.Sin(angle) * Math.Cos(angle) + Math.Pow(Math.Tan(angle), 2);
float hypot = Math.Sqrt(Math.Pow(3, 2) + Math.Pow(4, 2));

// --- 15. Deep Function Chains ---
int s1 = square(1);
float m = multiply(2.0, 3.0);
int val1 = addThree(s1, m, 4);
int deepChain = square(val1);
IO.Println("Deep chain: " + deepChain);

// --- 16. Full Expression Mix ---
float finalCalc = ((3 + 5) * (2 - 7) / 2 + Math.Pow(2, 3) - 4) / 2 + Math.Sqrt(16) - 1;
string mixed = "Result: " + finalCalc + ", Bool: " + logicTest + ", Msg: " + greet("Tester");

// --- 17. Edge Cases ---
string empty = "";
float zero = 0.0;
int negative = -42;
float negativeFloat = -3.14;
bool falseVal = false;
bool trueVal = true;
string specialChars = "!@#$%^&*()_+-=[]{}|;:'\\\",.<>/?";

// --- 18. A BIG While Loop ---
int x = 0;
while (x < 100000) {
    x++;
}

// --- 19. Printing everything ---
IO.Println("sum: " + sum + ", result: " + result + ", concat: " + concat);
IO.Println("complexBool: " + complexBool + ", testNegation: " + testNegation);
IO.Println("val: " + val + ", calc: " + calc + ", message: " + message);
IO.Println("complexCalc: " + complexCalc + ", nestedFold: " + nestedFold);
IO.Println("logicTest: " + logicTest + ", mod: " + mod + ", exp: " + exp);
IO.Println("angle: " + angle + ", trigTest: " + trigTest + ", hypot: " + hypot);
IO.Println("finalCalc: " + finalCalc + ", mixed: " + mixed);
IO.Println("empty: '" + empty + "', zero: " + zero + ", negative: " + negative + ", negativeFloat: " + negativeFloat);
IO.Println("falseVal: " + falseVal + ", trueVal: " + trueVal + ", specialChars: " + specialChars);
IO.Println("Big While Loop: " + x);
IO.Println("Hypotenuse: " + hypot);
int x1 = 10;
IO.Println("Type of x: " + x1.Type);
IO.Println("String of x: " + x1.ToString());
float f1 = 3.14;
IO.Println("Type of f: " + f1.Type);
IO.Println("String of f: " + f1.ToString());
// Chaining
IO.Println("Chained type: " + x1.ToString().Type);
// Module access
IO.Println("Sqrt of 16: " + Math.Sqrt(16));
```

**This program ran in:**
```
--- COMPILATION PHASE ---
 Lexing          | 1021  tokens   |   3.3328 ms
──────────────────────────────────────────────────
 Parsing         | 84    nodes    |   6.7881 ms
──────────────────────────────────────────────────
 Semantics       | 0     errors   |  11.9968 ms
──────────────────────────────────────────────────
 Bytecode Gen    | 435   ops      |   4.4066 ms
──────────────────────────────────────────────────

[√] Compilation finished in 29.5373 ms
```
**Execution:**
```
--- EXECUTION PHASE ---
[√] Execution finished in 16.6389 ms
```
**What the code actually printed**
```
Type of i: int
String of f: 3.14159
Length: 13
Hello, Vext!
Hello, Guy!
Line1\nLine2\tTabbed\"Quote\'Single
Comments ignored
i > 40
Even: 0
Even: 2
Even: 4
While: 0
While: 1
While: 2
Loop: 0
Loop: 1
Loop: 2
Deep chain: -9155818042444218343
sum: 57, result: 9.56636, concat: Hello, World! auto text 52 4.78318!
complexBool: True, testNegation: False
val: 12, calc: 40, message: Hello, Vext!
complexCalc: 14, nestedFold: 40
logicTest: True, mod: 1, exp: 1000
angle: 0.5, trigTest: 0.719181902813473, hypot: 5
finalCalc: -5, mixed: Result: -5, Bool: True, Msg: Hello, Tester!
empty: '', zero: 0, negative: -42, negativeFloat: -3.14
falseVal: False, trueVal: True, specialChars: !@#$%^&*()_+-=[]{}|;:'\",.<>/?
Big While Loop: 100000
Hypotenuse: 5
Type of x: int
String of x: 10
Type of f: float
String of f: 3.14
Chained type: string
Sqrt of 16: 4
```
**The total runtime was:**
```
=================================================================

Total Run Time: 46.1762 ms

=================================================================
```
**The final values of all variables:**
```
--- FINAL VM STATE ---
 Variable             │ Type       │ Value
─────────────────────────────────────────────────────────────────
i                    │ Int        │ 43
─────────────────────────────────────────────────────────────────
f                    │ Float      │ 3.14159
─────────────────────────────────────────────────────────────────
flag                 │ Bool       │ true
─────────────────────────────────────────────────────────────────
text                 │ String     │ Hello, World!
─────────────────────────────────────────────────────────────────
inferredInt          │ Int        │ 100
─────────────────────────────────────────────────────────────────
inferredFloat        │ Float      │ 0.25
─────────────────────────────────────────────────────────────────
inferredBool         │ Bool       │ false
─────────────────────────────────────────────────────────────────
inferredString       │ String     │ auto text
─────────────────────────────────────────────────────────────────
sum                  │ Int        │ 57
─────────────────────────────────────────────────────────────────
result               │ Float      │ 9.56636
─────────────────────────────────────────────────────────────────
concat               │ String     │ Hello, World! auto text 52 4.78318!
─────────────────────────────────────────────────────────────────
complexBool          │ Bool       │ true
─────────────────────────────────────────────────────────────────
testNegation         │ Bool       │ false
─────────────────────────────────────────────────────────────────
escaped              │ String     │ Line1\nLine2\tTabbed\"Quote\'Single
─────────────────────────────────────────────────────────────────
total                │ Int        │ 10
─────────────────────────────────────────────────────────────────
j                    │ Int        │ 5
─────────────────────────────────────────────────────────────────
k                    │ Int        │ 3
─────────────────────────────────────────────────────────────────
sq                   │ Int        │ 9
─────────────────────────────────────────────────────────────────
val                  │ Int        │ 12
─────────────────────────────────────────────────────────────────
calc                 │ Float      │ 40
─────────────────────────────────────────────────────────────────
message              │ String     │ Hello, Vext!
─────────────────────────────────────────────────────────────────
complexCalc          │ Float      │ 14
─────────────────────────────────────────────────────────────────
nestedFold           │ Int        │ 40
─────────────────────────────────────────────────────────────────
logicTest            │ Bool       │ true
─────────────────────────────────────────────────────────────────
a                    │ Int        │ 10
─────────────────────────────────────────────────────────────────
b                    │ Int        │ 3
─────────────────────────────────────────────────────────────────
mod                  │ Int        │ 1
─────────────────────────────────────────────────────────────────
exp                  │ Float      │ 1000
─────────────────────────────────────────────────────────────────
j                    │ Int        │ 3
─────────────────────────────────────────────────────────────────
angle                │ Float      │ 0.5
─────────────────────────────────────────────────────────────────
trigTest             │ Float      │ 0.719181902813473
─────────────────────────────────────────────────────────────────
hypot                │ Float      │ 5
─────────────────────────────────────────────────────────────────
s1                   │ Int        │ 1
─────────────────────────────────────────────────────────────────
m                    │ Float      │ 6
─────────────────────────────────────────────────────────────────
val1                 │ Int        │ 4618441417868443653
─────────────────────────────────────────────────────────────────
deepChain            │ Int        │ -9155818042444218343
─────────────────────────────────────────────────────────────────
finalCalc            │ Float      │ -5
─────────────────────────────────────────────────────────────────
mixed                │ String     │ Result: -5, Bool: True, Msg: Hello, Tester!
─────────────────────────────────────────────────────────────────
empty                │ String     │
─────────────────────────────────────────────────────────────────
zero                 │ Float      │ 0
─────────────────────────────────────────────────────────────────
negative             │ Int        │ -42
─────────────────────────────────────────────────────────────────
negativeFloat        │ Float      │ -3.14
─────────────────────────────────────────────────────────────────
falseVal             │ Bool       │ false
─────────────────────────────────────────────────────────────────
trueVal              │ Bool       │ true
─────────────────────────────────────────────────────────────────
specialChars         │ String     │ !@#$%^&*()_+-=[]{}|;:'\",.<>/?
─────────────────────────────────────────────────────────────────
x                    │ Int        │ 100000
─────────────────────────────────────────────────────────────────
x1                   │ Int        │ 10
─────────────────────────────────────────────────────────────────
f1                   │ Float      │ 3.14
─────────────────────────────────────────────────────────────────
n                    │ Int        │ 0
─────────────────────────────────────────────────────────────────
name                 │ Int        │ 0
─────────────────────────────────────────────────────────────────
a                    │ Int        │ 0
─────────────────────────────────────────────────────────────────
b                    │ Int        │ 0
─────────────────────────────────────────────────────────────────
a                    │ Int        │ 0
─────────────────────────────────────────────────────────────────
b                    │ Int        │ 0
─────────────────────────────────────────────────────────────────
c                    │ Int        │ 0
─────────────────────────────────────────────────────────────────
```

**Less interesting but:**
```
--- BYTECODE INSTRUCTIONS ---
 OP                   | ARG
─────────────────────────────────────────────────────
LOAD_CONST           │ 42
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 3.14159
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ True
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Hello, World!
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 100
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0.25
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ False
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ auto text
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 10
─────────────────────────────────────────────────────
ADD_INT              │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
MUL                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ 1.5
─────────────────────────────────────────────────────
SUB                  │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 10
─────────────────────────────────────────────────────
GT                   │
─────────────────────────────────────────────────────
JMP_IF_FALSE         │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 10
─────────────────────────────────────────────────────
LT                   │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ False
─────────────────────────────────────────────────────
JMP_IF_TRUE          │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
NOT                  │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ True
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Type of i:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ String of f:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Length:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
DEF_FUNC             │
─────────────────────────────────────────────────────
DEF_FUNC             │
─────────────────────────────────────────────────────
LOAD_CONST           │ Vext
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Guy
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
INC_VAR              │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 5
─────────────────────────────────────────────────────
ADD_INT              │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
MUL                  │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ !
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
NOT                  │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Line1\nLine2\tTabbed\"Quote\'Single
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Comments ignored
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 40
─────────────────────────────────────────────────────
GT                   │
─────────────────────────────────────────────────────
JMP_IF_FALSE         │
─────────────────────────────────────────────────────
LOAD_CONST           │ i > 40
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 42
─────────────────────────────────────────────────────
EQ                   │
─────────────────────────────────────────────────────
JMP_IF_FALSE         │
─────────────────────────────────────────────────────
LOAD_CONST           │ i == 42
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ i < 40
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
JMP_IF_VAR_OP_CONST  │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
ADD_INT              │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
MOD                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
EQ                   │
─────────────────────────────────────────────────────
JMP_IF_FALSE         │
─────────────────────────────────────────────────────
LOAD_CONST           │ Even:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
INC_VAR              │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
JMP_IF_VAR_OP_CONST  │
─────────────────────────────────────────────────────
LOAD_CONST           │ While:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
INC_VAR              │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
DEF_FUNC             │
─────────────────────────────────────────────────────
DEF_FUNC             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 1
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2.5
─────────────────────────────────────────────────────
LOAD_CONST           │ 4
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Vext
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 10
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
ADD_FLOAT            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 4
─────────────────────────────────────────────────────
SUB                  │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 1
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
ADD_INT              │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ True
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 10
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
MOD                  │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
JMP_IF_VAR_OP_CONST  │
─────────────────────────────────────────────────────
LOAD_CONST           │ Loop:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
INC_VAR              │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0.5
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
MUL                  │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
ADD_FLOAT            │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
LOAD_CONST           │ 4
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
ADD_FLOAT            │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 1
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
LOAD_CONST           │ 4
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Deep chain:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ -20
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
LOAD_CONST           │ 3
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
ADD_FLOAT            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 4
─────────────────────────────────────────────────────
SUB                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ 2
─────────────────────────────────────────────────────
DIV                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ 16
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
ADD_FLOAT            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 1
─────────────────────────────────────────────────────
SUB                  │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Result:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , Bool:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , Msg:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ Tester
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ -42
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ -3.14
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ False
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ True
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ !@#$%^&*()_+-=[]{}|;:'\",.<>/?
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 0
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
JMP_IF_VAR_OP_CONST  │
─────────────────────────────────────────────────────
INC_VAR              │
─────────────────────────────────────────────────────
JMP                  │
─────────────────────────────────────────────────────
LOAD_CONST           │ sum:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , result:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , concat:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ complexBool:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , testNegation:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ val:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , calc:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , message:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ complexCalc:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , nestedFold:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ logicTest:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , mod:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , exp:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ angle:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , trigTest:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , hypot:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ finalCalc:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , mixed:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ empty: '
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ ', zero:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , negative:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , negativeFloat:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ falseVal:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , trueVal:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_CONST           │ , specialChars:
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Big While Loop:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Hypotenuse:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 10
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Type of x:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ String of x:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ 3.14
─────────────────────────────────────────────────────
STORE_VAR            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Type of f:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ String of f:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Chained type:
─────────────────────────────────────────────────────
LOAD_VAR             │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
LOAD_CONST           │ Sqrt of 16:
─────────────────────────────────────────────────────
LOAD_CONST           │ 16
─────────────────────────────────────────────────────
CALL                 │
─────────────────────────────────────────────────────
CONCAT_STRING        │
─────────────────────────────────────────────────────
CALL_VOID            │
─────────────────────────────────────────────────────
```
