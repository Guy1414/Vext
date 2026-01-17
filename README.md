# Vext Programming Language

Created by [Guy Slutzker](https://github.com/Guy1414)

---

## 🛑 License & Usage
Currently, this project has **No License**.  

**All rights reserved.** This means:  
* **Viewing:** You are welcome to read the source code and learn from it.  
* **Contributions:** Suggestions and Pull Requests are welcome. By submitting a PR, you agree to allow the project owner to use your contribution.  
* **Copying/Stealing:** You do **not** have permission to copy, redistribute, or use this code in your own projects (commercial or non-commercial) without explicit written consent from Guy Slutzker.  
* **Derivatives:** You may not build or distribute modified versions of the Vext language.

© 2025 Guy Slutzker

---

## Overview

Vext is a programming language designed for performance, simplicity, and expressive syntax. It features full expressions, deep function calls, constant folding, and a custom virtual machine for execution.

---

## Features

### Core Language
- **Variables:** Declaration, use, type checking, `auto` type inference  
- **Types:** `int`, `float` (stored as double), `bool`, `string`, `auto`  
- **Expressions:** Nested arithmetic, boolean logic, comparisons, unary operators, function calls, mixed-type math  
- **Operators:**  
  - Arithmetic: `+ - * / % **`  
  - Comparison: `== != < > <= >=`  
  - Logic: `&& || !`  
  - Unary: `++ -- -`  
  - Assignment / Compound: `= += -= *= /=`  
  - String concatenation: `+` (works with numbers and booleans)  
- **Strings:** Escape sequences (`\n`, `\t`, `\"`, `\'`, `\\`), automatic conversion when concatenating other types  

### Control Flow
- `if / else if / else`  
- `while` loops  
- `for` loops  
- Nested loops supported  

### Functions
- Function declaration with typed parameters and return type  
- `auto` parameters supported  
- Nested function calls and expression evaluation  
- Return statements  

### Constant Folding & Compile-Time Optimization
- Nested expressions evaluated at compile time  
- Binary and unary operations folded  
- Boolean short-circuiting handled  
- Strings and numeric types automatically folded  

### Standard Library
- `print()` for console output
- `len()` for getting length of string
- 
- Math functions: `Math.pow(float num, float power)`, `Math.sqrt(float num)`, `Math.sin()`, `Math.cos()`, `Math.tan()`, `Math.log()`, `Math.exp()`, `Math.random()`, `Math.random(float min, float max)`, `Math.abs(float num)`, `Math.round(float num)`, `Math.floor(float num)`, `Math.ceil(float num)`, `Math.min(float num)`, `Math.max(float num)`

### Compiler Architecture
Vext features a **full compilation pipeline**:
1. **Lexer:** Tokenizes source code  
2. **Parser:** Builds an abstract syntax tree (AST)  
3. **Semantic Pass:** Type checking, variable resolution, constant folding  
4. **Bytecode Generator:** Converts AST into Vext bytecode  
5. **VextVM:** Executes bytecode efficiently  

---

## Abstract Syntax Tree (AST) Node Types

**Expressions:**
- `ExpressionNode` — base type for all expressions  
- `BinaryExpressionNode` — binary ops: `+ - * / **`  
- `UnaryExpressionNode` — unary ops: `++ -- - !`  
- `LiteralNode` — numbers, strings, booleans  
- `VariableNode` — identifiers  
- `FunctionCallNode` — function calls  
- `ModuleAccessNode` — module functions  

**Statements:**
- `StatementNode` — base type  
- `ExpressionStatementNode` — e.g., `x + 1;`  
- `VariableDeclarationNode`  
- `IfStatementNode`  
- `WhileStatementNode`  
- `ForStatementNode`  
- `ReturnStatementNode`  
- `AssignmentStatementNode`  
- `IncrementStatementNode`  
- `FunctionDefinitionNode`  

**Function Parameters:**
- `FunctionParameterNode` — typed parameters with optional initializers  

---

**Example Program**
```// --- 1. Basic Types & Declarations ---
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

// --- 3. Unary & Compound Operators ---
i++;
--f;
sum += 5;
result *= 2.0;
concat += "!";
bool testNegation = !complexBool;

// --- 4. Strings & Escapes ---
string escaped = "Line1\\nLine2\\tTabbed\\\"Quote\\'Single";
print(escaped);

// --- 5. Comments ---
print("Comments ignored");

// --- 6. Conditionals ---
if (i > 40) {
    print("i > 40");
} else if (i == 42) {
    print("i == 42");
} else {
    print("i < 40");
}

// --- 7. Loops ---
int total = 0;
for (int j = 0; j < 5; j++) {
    total += j;
    if (j % 2 == 0) print("Even: " + j);
}
int k = 0;
while (k < 3) {
    print("While: " + k);
    k++;
}

// --- 8. Functions & Nested Calls ---
int square(int n) { return n * n; }
float multiply(float a, float b) { return a * b; }
string greet(string name) { return "Hello, " + name + "!"; }
int addThree(auto a, auto b, auto c) { return a + b + c; } // auto allows int/float mix

int sq = square(3);
int val = addThree(1, 2, sq);
float calc = multiply(2.5, square(4));
string message = greet("Vext");

// --- 9. Nested Expressions & Constant Folding ---
float complexCalc = ((2 + 3) * (5 - 1) / 2) + Math.pow(2, 3) - 4;
int nestedFold = square(addThree(1, 2, 3)) + square(2);

// --- 10. Booleans & Logic ---
bool logicTest = (true && false) || (false || true) && !false;

// --- 11. Advanced Operators ---
int a = 10;
int b = 3;
int mod = a % b;
float exp = Math.pow(a, b); // 10^3

// --- 14. Math & Trigonometry ---
float angle = 0.5;
float trigTest = Math.sin(angle) * Math.cos(angle) + Math.pow(Math.tan(angle), 2);
float hypot = Math.sqrt(Math.pow(3, 2) + Math.pow(4, 2));

// --- 15. Deep Function Chains ---
int s1 = square(1);
float m = multiply(2.0, 3.0);
int val1 = addThree(s1, m, 4);
int deepChain = square(val1);
print("Deep chain: " + deepChain);

// --- 16. Full Expression Mix ---
float finalCalc = ((3 + 5) * (2 - 7) / 2 + Math.pow(2, 3) - 4) / 2 + Math.sqrt(16) - 1;
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

// --- 19. printing everything ---
print("sum: " + sum + ", result: " + result + ", concat: " + concat);
print("complexBool: " + complexBool + ", testNegation: " + testNegation);
print("val: " + val + ", calc: " + calc + ", message: " + message);
print("complexCalc: " + complexCalc + ", nestedFold: " + nestedFold);
print("logicTest: " + logicTest + ", mod: " + mod + ", exp: " + exp);
print("angle: " + angle + ", trigTest: " + trigTest + ", hypot: " + hypot);
print("finalCalc: " + finalCalc + ", mixed: " + mixed);
print("empty: '" + empty + "', zero: " + zero + ", negative: " + negative + ", negativeFloat: " + negativeFloat);
print("falseVal: " + falseVal + ", trueVal: " + trueVal + ", specialChars: " + specialChars);
print("Big While Loop: " + x);
```

**This program ran in:**
```
--- COMPILATION PHASE ---
 Lexing    :  783 tokens   |   3.4101 ms
 Parsing   :   68 nodes    |  13.7634 ms
 Semantics :    0 errors   |   8.2346 ms
 CodeGen   :  362 ops      |   2.8305 ms

--- EXECUTION PHASE ---
 [√] VM finished in 11.0824 ms

=============================================
Total Process Time: 53.68 ms
=============================================
```
```
--- FINAL VM STATE ---
 Variable     | Type       | Value
---------------------------------------------
 Variable        | Type       | Value
--------------------------------------------------
 i               | Number     | 43
 f               | Number     | 3.14159
 flag            | Bool       | true
 text            | String     | Hello, World!
 inferredInt     | Number     | 100
 inferredFloat   | Number     | 0.25
 inferredBool    | Bool       | false
 inferredString  | String     | auto text
 sum             | Number     | 52
 result          | Number     | 9.56636
 concat          | String     | Hello, World! auto text 52 4.78318!
 complexBool     | Bool       | true
 testNegation    | Bool       | false
 escaped         | String     | Line1\nLine2\tTabbed\"Quote\'Single
 total           | Number     | 10
 j               | Number     | 5
 k               | Number     | 3
 sq              | Number     | 9
 val             | Number     | 12
 calc            | Number     | 40
 message         | String     | Hello, Vext!
 complexCalc     | Number     | 14
 nestedFold      | Number     | 40
 logicTest       | Bool       | true
 a               | Number     | 10
 b               | Number     | 3
 mod             | Number     | 1
 exp             | Number     | 1000
 angle           | Number     | 0.5
 trigTest        | Number     | 0.719181902813473
 hypot           | Number     | 5
 s1              | Number     | 1
 m               | Number     | 6
 val1            | Number     | 11
 deepChain       | Number     | 121
 finalCalc       | Number     | -5
 mixed           | String     | Result: -5, Bool: 0, Msg: Hello, Tester!
 empty           | String     |
 zero            | Number     | 0
 negative        | Number     | -42
 negativeFloat   | Number     | -3.14
 falseVal        | Bool       | false
 trueVal         | Bool       | true
 specialChars    | String     | !@#$%^&*()_+-=[]{}|;:'\",.<>/?
 x               | Number     | 100000
 n               | Number     | 0
 a               | Number     | 0
 b               | Number     | 0
 name            | Number     | 0
 a               | Number     | 0
 b               | Number     | 0
 c               | Number     | 0
 Slot 52         | Number     | 0
 Slot 53         | Number     | 0
 Slot 54         | Number     | 0
 Slot 55         | Number     | 0
 Slot 56         | Number     | 0
 Slot 57         | Number     | 0
 Slot 58         | Number     | 0
 Slot 59         | Number     | 0
 Slot 60         | Number     | 0
 Slot 61         | Number     | 0
 Slot 62         | Number     | 0
 Slot 63         | Number     | 0
```
