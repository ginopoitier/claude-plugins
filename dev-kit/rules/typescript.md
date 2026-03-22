# Rule: TypeScript Conventions

## DO
- Strict mode always on: `"strict": true` in `tsconfig.json`
- Define explicit interfaces/types for all API response shapes
- Use `unknown` when the type is genuinely unknown — then narrow it
- Use discriminated unions for state: `{ status: 'loading' } | { status: 'success'; data: T } | { status: 'error'; message: string }`
- Use `as const` for static lookup objects and config
- Prefer `interface` for object shapes that may be extended; `type` for unions, intersections, aliases
- Use `readonly` on properties that shouldn't change after construction
- Export types explicitly: `export type { Order, OrderStatus }` separate from value exports

## DON'T
- Don't use `any` — use `unknown` + type narrowing or proper generics
- Don't use `@ts-ignore` without a comment explaining the specific reason
- Don't use `as Type` casts for non-obvious cases — narrow with type guards
- Don't export both a class and its instance type with the same name
- Don't use `enum` — use `as const` objects with a derived type:
  ```ts
  const OrderStatus = { Pending: 'Pending', Cancelled: 'Cancelled' } as const
  type OrderStatus = typeof OrderStatus[keyof typeof OrderStatus]
  ```
- Don't use `Function` as a type — use explicit function signatures
- Don't ignore unhandled promise rejections — always `await` or `.catch()`
