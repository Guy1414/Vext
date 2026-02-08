# Codebase Audit: Proposed Fix Tasks

## 1) Typos / wording cleanup

### Task T1 — Fix example section numbering and wording in docs/samples
- **Why:** The long example jumps from section `11` to `14`, and one heading is lower-cased (`printing everything`), which looks like a typo/consistency miss in both user-facing docs and the sample program.
- **Evidence:** `README.md` uses `// --- 11. Advanced Operators ---` then `// --- 14. Math & Trigonometry ---`, and later `// --- 19. printing everything ---`.
- **Scope:**
  - Renumber the section comments sequentially (11 → 12, 15 → 13, etc. as appropriate).
  - Normalize heading capitalization.
  - Mirror the exact same fixes inside `Vext.TestRunner/Program.cs` to keep sample parity.

## 2) Bug fixes

### Task B1 — Restore runtime output for `print()`
- **Why:** The built-in `print` function currently returns an empty string and does not output anything to stdout.
- **Evidence:** `Vext/Modules/DefaultFunctions.cs` has `//Console.WriteLine(args[0]?.ToString());` commented out.
- **Fix idea:** Re-enable output (`Console.WriteLine`) and return a proper `void` sentinel (or `null`) according to VM expectations.

### Task B2 — Implement `VextValueConverter.Read` (or remove unsupported deserialization path)
- **Why:** `Read` throws `NotImplementedException`, which can crash LSP JSON handling if deserialization is ever triggered.
- **Evidence:** `Vext.LSP/VextValueConverter.cs` line 11 throws unconditionally.
- **Fix idea:** Parse `{ type, value }` safely into `VextValue`, and add validation/errors for malformed payloads.

### Task B3 — Improve type-mismatch diagnostic detail
- **Why:** A placeholder diagnostic (`"Type mismatch..."`) makes real semantic issues harder to debug.
- **Evidence:** `Vext/Semantic/SemanticPass.cs` reports `Type mismatch...` when initializer and declared type conflict.
- **Fix idea:** Include expected type, actual type, and variable name in the message.

## 3) Comments / documentation discrepancies

### Task D1 — Align README standard library signatures with implementation
- **Why:** README documents math APIs with incorrect arity/signatures versus implementation.
- **Evidence:**
  - README lists `Math.sin()`, `Math.cos()`, `Math.tan()`, `Math.log()`, `Math.exp()` with no parameters and `Math.min(float num)`, `Math.max(float num)`.
  - Implementation defines each trig/log/exp function with **1** argument and min/max with **2** arguments.
- **Fix idea:** Update README signatures to match actual runtime behavior.

### Task D2 — Clarify `len()` wording in docs
- **Why:** Wording currently says `length of string`; clearer phrasing is `length of a string`, and should explicitly mention non-string input errors.
- **Evidence:** README standard library bullet says `len() for getting length of string`, while implementation throws on non-string input.
- **Fix idea:** Update README text and include one short example/error note.

## 4) Test improvement (one targeted task)

### Task Q1 — Add a non-interactive regression test project for built-ins and diagnostics
- **Why:** Current `Vext.TestRunner` is interactive (`Console.ReadLine`, `notepad.exe`) and unsuitable for CI/regression automation.
- **Evidence:** `Vext.TestRunner/Program.cs` opens Notepad and waits for Enter multiple times.
- **Proposal:**
  - Add an automated test project (e.g., xUnit/NUnit) that compiles and runs small snippets via `VextEngine`.
  - Include at least one assertion for:
    1. `print()` producing observable output,
    2. `Math.min/max` arity validation,
    3. detailed type mismatch diagnostics.
