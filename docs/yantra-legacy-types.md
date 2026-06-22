# Legacy Yantra JS Type Names (Y-prefix)

This document records all class, enum, and related type names starting with `Y` that
remained from the original Yantra JS codebase before being renamed to the `B` prefix
as part of the Broiler.JS clean-up.

All 74 types lived in a single subsystem: the **expression compiler IR** under
`Broiler.JS/Broiler.JavaScript.ExpressionCompiler/Expressions/`, with minor spillover
into `Broiler.JavaScript.LinqExpressions/`.

> **Status: renamed.** All types and source files below have been renamed from `Y*` to
> `B*` (e.g. `YExpression` → `BExpression`, `YExpression.cs` → `BExpression.cs`).
> This document is kept as a historical record of what was changed and why.

---

## Why `B` and not no-prefix

`System.Linq.Expressions` already defines `BlockExpression`, `BinaryExpression`,
`LambdaExpression`, `ConditionalExpression`, and many more. Dropping the prefix would
cause compile conflicts throughout the codebase. `B` (for **Broiler**) has zero
collisions.

---

## Enums (3)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YExpressionType` | `BExpressionType` | `BExpressionType.cs` | Node-type discriminator for the IR |
| `YOperator` | `BOperator` | `BOperator.cs` | Binary operator variants |
| `YUnaryOperator` | `BUnaryOperator` | `BUnaryOperator.cs` | Unary operator variants |

---

## Abstract / Visitor Base Classes (4)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YExpression` | `BExpression` | `BExpression.cs` | Abstract base for all IR nodes |
| `YExpression<T>` | `BExpression<T>` | `BExpressionOfT.cs` | Generic typed expression (function declarations) |
| `YExpressionVisitor<T>` | `BExpressionVisitor<T>` | `BExpressionVisitor.cs` | Abstract visitor over the IR tree |
| `YExpressionMapVisitor` | `BExpressionMapVisitor` | `BExpressionMapVisitor.cs` | Tree-to-tree mapping visitor |

---

## Control Flow (6)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YBlockExpression` | `BBlockExpression` | `BBlockExpression.cs` | Sequential block of expressions |
| `YConditionalExpression` | `BConditionalExpression` | `BConditionalExpression.cs` | If-then-else / ternary |
| `YLoopExpression` | `BLoopExpression` | `BLoopExpression.cs` | Loop construct |
| `YSwitchExpression` | `BSwitchExpression` | `BSwitchExpression.cs` | Switch statement |
| `YSwitchCaseExpression` | `BSwitchCaseExpression` | `BSwitchCaseExpression.cs` | Individual switch case |
| `YJumpSwitchExpression` | `BJumpSwitchExpression` | `BJumpSwitchExpression.cs` | Jump-table-optimised switch |

---

## Labels and Jumps (4)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YLabelTarget` | `BLabelTarget` | `BLabelTarget.cs` | Named jump target |
| `YLabelExpression` | `BLabelExpression` | `BLabelExpression.cs` | Label placement in the IR |
| `YGoToExpression` | `BGoToExpression` | `BLabelExpression.cs` | Goto / unconditional jump |
| `YReturnExpression` | `BReturnExpression` | `BLabelExpression.cs` | Return from function |

---

## Exception Handling (3)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YTryCatchFinallyExpression` | `BTryCatchFinallyExpression` | `BTryCatchFinallyExpression.cs` | Try / catch / finally block |
| `YCatchBody` | `BCatchBody` | `BTryCatchFinallyExpression.cs` | Catch clause data |
| `YThrowExpression` | `BThrowExpression` | `BThrowExpression.cs` | Throw expression |

---

## Functions and Lambdas (4)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YLambdaExpression` | `BLambdaExpression` | `BLambdaExpression.cs` | Lambda / anonymous function |
| `YDelegateExpression` | `BDelegateExpression` | `BDelegateExpression.cs` | Delegate-typed expression |
| `YCallExpression` | `BCallExpression` | `BCallExpression.cs` | Static/virtual method call |
| `YInvokeExpression` | `BInvokeExpression` | `BInvokeExpression.cs` | Delegate / functor invoke |

---

## Member Access (5)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YParameterExpression` | `BParameterExpression` | `BParameterExpression.cs` | Named function parameter |
| `YFieldExpression` | `BFieldExpression` | `BFieldExpression.cs` | Field read / write |
| `YPropertyExpression` | `BPropertyExpression` | `BPropertyExpression.cs` | Property read / write |
| `YIndexExpression` | `BIndexExpression` | `BIndexExpression.cs` | Indexer access |
| `YArrayIndexExpression` | `BArrayIndexExpression` | `BArrayIndexExpression.cs` | Direct array element access |

---

## Binary / Unary Operations (2)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YBinaryExpression` | `BBinaryExpression` | `BBinaryExpression.cs` | Binary operation (left op right) |
| `YUnaryExpression` | `BUnaryExpression` | `BUnaryExpression.cs` | Unary operation |

---

## Type Operations and Conversions (5)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YTypeIsExpression` | `BTypeIsExpression` | `BTypeIsExpression.cs` | `is` type check |
| `YTypeAsExpression` | `BTypeAsExpression` | `BTypeAsExpression.cs` | `as` type cast |
| `YConvertExpression` | `BConvertExpression` | `BConvertExpression.cs` | Explicit type conversion |
| `YBoxExpression` | `BBoxExpression` | `BBoxExpression.cs` | Box value type to object |
| `YUnboxExpression` | `BUnboxExpression` | `BUnboxExpression.cs` | Unbox object to value type |

---

## Constant Expressions (12)

All defined in `BInt32ConstantExpression.cs` except the first.

| Old name | New name | Notes |
|---|---|---|
| `YConstantExpression` | `BConstantExpression` | Generic / object constant |
| `YInt32ConstantExpression` | `BInt32ConstantExpression` | `int` constant |
| `YUInt32ConstantExpression` | `BUInt32ConstantExpression` | `uint` constant |
| `YInt64ConstantExpression` | `BInt64ConstantExpression` | `long` constant |
| `YUInt64ConstantExpression` | `BUInt64ConstantExpression` | `ulong` constant |
| `YDoubleConstantExpression` | `BDoubleConstantExpression` | `double` constant |
| `YFloatConstantExpression` | `BFloatConstantExpression` | `float` constant |
| `YBooleanConstantExpression` | `BBooleanConstantExpression` | `bool` constant |
| `YByteConstantExpression` | `BByteConstantExpression` | `byte` constant |
| `YStringConstantExpression` | `BStringConstantExpression` | `string` constant |
| `YTypeConstantExpression` | `BTypeConstantExpression` | `Type` constant |
| `YMethodConstantExpression` | `BMethodConstantExpression` | `MethodInfo` constant |

---

## Arrays and Collections (4)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YArrayLengthExpression` | `BArrayLengthExpression` | `BArrayLengthExpression.cs` | `.Length` on a 1-D array |
| `YNewArrayExpression` | `BNewArrayExpression` | `BNewArrayExpression.cs` | `new T[]` allocation |
| `YNewArrayBoundsExpression` | `BNewArrayBoundsExpression` | `BNewArrayBoundsExpression.cs` | Multi-dimensional `new T[a,b]` |
| `YListInitExpression` | `BListInitExpression` | `BListInitExpression.cs` | Collection initializer |

---

## Object Creation and Initialization (6)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YNewExpression` | `BNewExpression` | `BNewExpression.cs` | `new T(...)` |
| `YMemberInitExpression` | `BMemberInitExpression` | `BMemberInitExpression.cs` | `new T { Prop = ... }` |
| `YBinding` | `BBinding` | `BMemberAssignment.cs` | Abstract member binding |
| `YElementInit` | `BElementInit` | `BMemberAssignment.cs` | Collection element initializer |
| `YMemberElementInit` | `BMemberElementInit` | `BMemberAssignment.cs` | Member-level element init |
| `YMemberAssignment` | `BMemberAssignment` | `BMemberAssignment.cs` | `Member = value` assignment |

---

## Assignment and Special Operators (3)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YAssignExpression` | `BAssignExpression` | `BAssignExpression.cs` | Assignment (`=`) |
| `YCoalesceExpression` | `BCoalesceExpression` | `BCoalesceExpression.cs` | Null-coalescing (`??`) |
| `YCoalesceCallExpression` | `BCoalesceCallExpression` | `BCoalesceCallExpression.cs` | Null-coalescing with call |

---

## Miscellaneous IR Nodes (5)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YEmptyExpression` | `BEmptyExpression` | `BEmptyExpression.cs` | No-op placeholder |
| `YAddressOfExpression` | `BAddressOfExpression` | `BAddressOfExpression.cs` | `&x` address-of |
| `YDebugInfoExpression` | `BDebugInfoExpression` | `BDebugInfoExpression.cs` | Debug sequence-point marker |
| `YILOffsetExpression` | `BILOffsetExpression` | `BILOffsetExpression.cs` | IL offset marker |
| `YYieldExpression` | `BYieldExpression` | `BYieldExpression.cs` | `yield return` / generator yield |

---

## Builder and Extension Helpers (2)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YBlockBuilder` | `BBlockBuilder` | `BBlockBuilder.cs` | Fluent builder for `BBlockExpression` |
| `YExpressionExtensions` | `BExpressionExtensions` | `BExpressionExtensions.cs` | Extension methods on IR nodes |

---

## Dispatcher (1)

| Old name | New name | File | Notes |
|---|---|---|---|
| `YDispatcher` | `BDispatcher` | `StackGuard.cs` | Stack-guard dispatcher for visitors |

---

## Not renamed (2)

These live in `Broiler.JavaScript.LinqExpressions` and use `Yield` as a JavaScript
domain word, not as a Yantra prefix.

| Name | File | Notes |
|---|---|---|
| `YieldFinderHelper` | `YieldFinderHelper.cs` | Static helper — locates yield points for generator rewriting |
| `YieldFinder` | `YieldFinderHelper.cs` | Nested visitor class inside `YieldFinderHelper` |
