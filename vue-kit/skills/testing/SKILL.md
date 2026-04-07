---
name: testing
description: >
  Testing strategy for Vue 3 with Vitest, @vue/test-utils, and createTestingPinia.
  Load this skill when: "test", "testing", "vitest", "test-utils", "unit test", "component test",
  "store test", "composable test", "mock api", "vi.mock", "/testing".
user-invocable: true
argument-hint: "[component or feature to test]"
allowed-tools: Read, Write, Edit, Bash, Glob, Grep
---

# Testing — Vitest + Vue Test Utils Strategy

## Core Principles

1. **Arrange–Act–Assert in every test** — Each test has a clear setup block, a single action, and focused assertions. One assertion concept per test is preferred.
2. **Test behavior, not implementation** — Assert what the user sees and what events are emitted. Never assert on internal ref values or store internals directly.
3. **Always unmount after each test** — Call `wrapper.unmount()` in `afterEach` to prevent memory leaks and listener accumulation between tests.
4. **Mock the API layer, not axios** — Use `vi.mock('@/features/products/api')` to stub the feature's API module. Never mock axios directly — that couples tests to the HTTP implementation.
5. **`createTestingPinia` for store tests** — Use Pinia's testing utility to get a fresh store per test with controlled initial state.
6. **No real network calls in tests** — All `fetch`/`axios` calls must be intercepted. `vi.mock` and MSW are the two approved approaches.

## Test File Conventions

| What you're testing | File location | Naming |
|---------------------|--------------|--------|
| Component | Next to component | `ProductCard.spec.ts` |
| Composable | Next to composable | `useCounter.spec.ts` |
| Pinia store | Next to store | `productStore.spec.ts` |
| API client | Next to api.ts | `api.spec.ts` |
| Integration (page) | `tests/integration/` | `ProductListPage.spec.ts` |

## Patterns

### Component Test

```typescript
// features/products/components/ProductCard.spec.ts
import { describe, it, expect, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import ProductCard from './ProductCard.vue'

const defaultProps = {
  productId: 'p-1',
  name: 'Wireless Keyboard',
  price: 89.99,
  imageUrl: null,
  inStock: true,
}

describe('ProductCard', () => {
  let wrapper: ReturnType<typeof mount>

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders the product name and formatted price', () => {
    wrapper = mount(ProductCard, { props: defaultProps })

    expect(wrapper.text()).toContain('Wireless Keyboard')
    expect(wrapper.text()).toContain('$89.99')
  })

  it('disables the add-to-cart button when out of stock', async () => {
    wrapper = mount(ProductCard, { props: { ...defaultProps, inStock: false } })

    const button = wrapper.find('button[type="button"]')
    expect(button.attributes('disabled')).toBeDefined()
  })

  it('emits addToCart with the product ID when button is clicked', async () => {
    wrapper = mount(ProductCard, { props: defaultProps })

    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('addToCart')).toHaveLength(1)
    expect(wrapper.emitted('addToCart')![0]).toEqual(['p-1'])
  })

  it('shows out-of-stock label when inStock is false', () => {
    wrapper = mount(ProductCard, { props: { ...defaultProps, inStock: false } })

    expect(wrapper.text()).toContain('Out of stock')
  })

  it('does not emit addToCart when out of stock and button clicked', async () => {
    wrapper = mount(ProductCard, { props: { ...defaultProps, inStock: false } })

    await wrapper.find('button').trigger('click')

    expect(wrapper.emitted('addToCart')).toBeUndefined()
  })
})
```

### Pinia Store Test

```typescript
// features/products/stores/productStore.spec.ts
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useProductStore } from './productStore'
import * as api from '@/features/products/api'
import type { Product } from '@/features/products/api'

vi.mock('@/features/products/api')

const mockProduct: Product = {
  id: 'p-1',
  name: 'Keyboard',
  description: 'Mechanical keyboard',
  price: 89.99,
  stock: 10,
  categoryId: 'cat-1',
  imageUrl: null,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
}

describe('productStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.resetAllMocks()
  })

  describe('fetchAll', () => {
    it('populates products on success', async () => {
      vi.mocked(api.fetchProducts).mockResolvedValue({
        items: [mockProduct],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      })

      const store = useProductStore()
      await store.fetchAll()

      expect(store.products).toHaveLength(1)
      expect(store.products[0].name).toBe('Keyboard')
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })

    it('sets error message on API failure', async () => {
      vi.mocked(api.fetchProducts).mockRejectedValue(new Error('Network error'))

      const store = useProductStore()
      await store.fetchAll()

      expect(store.products).toHaveLength(0)
      expect(store.error).toBe('Network error')
      expect(store.isLoading).toBe(false)
    })

    it('sets isLoading to true while fetching', async () => {
      let resolvePromise!: (value: { items: Product[]; totalCount: number; page: number; pageSize: number }) => void
      vi.mocked(api.fetchProducts).mockReturnValue(
        new Promise((r) => { resolvePromise = r }),
      )

      const store = useProductStore()
      const fetchPromise = store.fetchAll()

      expect(store.isLoading).toBe(true)

      resolvePromise({ items: [], totalCount: 0, page: 1, pageSize: 20 })
      await fetchPromise

      expect(store.isLoading).toBe(false)
    })
  })

  describe('remove', () => {
    it('removes the product optimistically', async () => {
      vi.mocked(api.deleteProduct).mockResolvedValue(undefined)

      const store = useProductStore()
      store.products = [mockProduct]
      await store.remove('p-1')

      expect(store.products).toHaveLength(0)
    })

    it('rolls back on API failure', async () => {
      vi.mocked(api.deleteProduct).mockRejectedValue(new Error('Delete failed'))

      const store = useProductStore()
      store.products = [mockProduct]

      await expect(store.remove('p-1')).rejects.toThrow('Delete failed')
      expect(store.products).toHaveLength(1)
    })
  })
})
```

### Component Test with Pinia (createTestingPinia)

```typescript
// features/products/pages/ProductListPage.spec.ts
import { describe, it, expect, afterEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createTestingPinia } from '@pinia/testing'
import ProductListPage from './ProductListPage.vue'
import { useProductStore } from '@/features/products/stores/productStore'
import type { Product } from '@/features/products/api'

const mockProducts: Product[] = [
  { id: '1', name: 'Keyboard', price: 89.99, stock: 5, description: '', categoryId: 'c1', imageUrl: null, createdAt: '', updatedAt: '' },
  { id: '2', name: 'Mouse', price: 49.99, stock: 0, description: '', categoryId: 'c1', imageUrl: null, createdAt: '', updatedAt: '' },
]

describe('ProductListPage', () => {
  let wrapper: ReturnType<typeof mount>

  afterEach(() => wrapper.unmount())

  it('calls fetchAll on mount', () => {
    wrapper = mount(ProductListPage, {
      global: {
        plugins: [createTestingPinia({ createSpy: vi.fn })],
      },
    })

    const store = useProductStore()
    expect(store.fetchAll).toHaveBeenCalledOnce()
  })

  it('renders a card for each product', () => {
    wrapper = mount(ProductListPage, {
      global: {
        plugins: [
          createTestingPinia({
            createSpy: vi.fn,
            initialState: {
              products: { products: mockProducts, isLoading: false, error: null },
            },
          }),
        ],
      },
    })

    // Each product renders a list item
    expect(wrapper.findAll('li')).toHaveLength(2)
    expect(wrapper.text()).toContain('Keyboard')
    expect(wrapper.text()).toContain('Mouse')
  })

  it('shows a loading spinner while fetching', () => {
    wrapper = mount(ProductListPage, {
      global: {
        plugins: [
          createTestingPinia({
            createSpy: vi.fn,
            initialState: { products: { isLoading: true } },
          }),
        ],
      },
    })

    // Loading spinner is visible
    expect(wrapper.find('[data-testid="loading-spinner"]').exists()).toBe(true)
  })
})
```

### Composable Test

```typescript
// composables/useCounter.spec.ts
import { describe, it, expect } from 'vitest'
import { useCounter } from './useCounter'

describe('useCounter', () => {
  it('initializes with default value of 0', () => {
    const { count } = useCounter()
    expect(count.value).toBe(0)
  })

  it('initializes with the provided value', () => {
    const { count } = useCounter({ initial: 10 })
    expect(count.value).toBe(10)
  })

  it('increments count', () => {
    const { count, increment } = useCounter()
    increment()
    expect(count.value).toBe(1)
  })

  it('does not exceed max', () => {
    const { count, increment } = useCounter({ initial: 9, max: 10 })
    increment()
    increment()
    expect(count.value).toBe(10)
  })

  it('resets to initial value', () => {
    const { count, increment, reset } = useCounter({ initial: 5 })
    increment()
    increment()
    reset()
    expect(count.value).toBe(5)
  })

  it('isAtMax is true when count equals max', () => {
    const { isAtMax, increment } = useCounter({ initial: 9, max: 10 })
    increment()
    expect(isAtMax.value).toBe(true)
  })
})
```

### Composable with Lifecycle (using `withSetup`)

```typescript
// composables/useDebounce.spec.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import { mount } from '@vue/test-utils'
import { useDebounce } from './useDebounce'

// Helper to run a composable inside a component's setup context
function withSetup<T>(composable: () => T): [T, ReturnType<typeof mount>] {
  let result!: T
  const wrapper = mount({
    setup() {
      result = composable()
      return () => null
    },
  })
  return [result, wrapper]
}

describe('useDebounce', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('returns the initial value immediately', () => {
    const source = ref('hello')
    const [debounced] = withSetup(() => useDebounce(source, 300))

    expect(debounced.value).toBe('hello')
  })

  it('does not update before the delay expires', async () => {
    const source = ref('hello')
    const [debounced] = withSetup(() => useDebounce(source, 300))

    source.value = 'world'
    await vi.advanceTimersByTimeAsync(100)

    expect(debounced.value).toBe('hello')
  })

  it('updates after the delay expires', async () => {
    const source = ref('hello')
    const [debounced] = withSetup(() => useDebounce(source, 300))

    source.value = 'world'
    await vi.advanceTimersByTimeAsync(300)

    expect(debounced.value).toBe('world')
  })
})
```

### API Module Test (with vi.mock)

```typescript
// features/products/api.spec.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { fetchProductById } from './api'
import { apiClient } from '@/lib/apiClient'

vi.mock('@/lib/apiClient', () => ({
  apiClient: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}))

describe('fetchProductById', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('calls the correct endpoint', async () => {
    vi.mocked(apiClient.get).mockResolvedValue({ data: { id: 'p-1', name: 'Keyboard' } })

    await fetchProductById('p-1')

    expect(apiClient.get).toHaveBeenCalledWith('/products/p-1')
  })

  it('returns the response data', async () => {
    const mockProduct = { id: 'p-1', name: 'Keyboard', price: 89.99 }
    vi.mocked(apiClient.get).mockResolvedValue({ data: mockProduct })

    const result = await fetchProductById('p-1')

    expect(result).toEqual(mockProduct)
  })
})
```

## Vitest Configuration

```typescript
// vitest.config.ts
import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./tests/setup.ts'],
  },
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src'),
    },
  },
})
```

```typescript
// tests/setup.ts
import { config } from '@vue/test-utils'
import { createPinia } from 'pinia'

// Global plugins applied to every mount() call
config.global.plugins = [createPinia()]
```

## Anti-patterns

### Testing implementation details

```typescript
// BAD — tests the internal ref, not user-visible behavior
it('sets count ref to 5', () => {
  const store = useProductStore()
  store.products = mockProducts
  expect(store.products.length).toBe(mockProducts.length)  // fine actually, but...
  // BAD: asserting internal ref directly instead of behavior
  expect(store.$state.isLoading).toBe(false)
})

// GOOD — test observable effects
it('renders product count in the heading', () => {
  // ... mount with createTestingPinia, assert rendered text
})
```

### Not unmounting after tests

```typescript
// BAD — wrapper accumulates across tests, event listeners leak
describe('ProductCard', () => {
  const wrapper = mount(ProductCard, { props: defaultProps })  // created once for all tests

  it('...', () => { /* ... */ })
})

// GOOD
describe('ProductCard', () => {
  let wrapper: ReturnType<typeof mount>
  afterEach(() => wrapper.unmount())
  it('...', () => { wrapper = mount(ProductCard, { props: defaultProps }) })
})
```

### Real API calls in tests

```typescript
// BAD — flaky, slow, network dependent
it('fetches products', async () => {
  const store = useProductStore()
  await store.fetchAll()  // hits real /api/products endpoint
  expect(store.products.length).toBeGreaterThan(0)
})

// GOOD
vi.mock('@/features/products/api')
vi.mocked(api.fetchProducts).mockResolvedValue({ items: mockProducts, ... })
```

## Decision Guide

| What to test | Tool |
|-------------|------|
| Props, slots, emitted events | `@vue/test-utils` `mount` |
| Pinia store logic in isolation | `setActivePinia(createPinia())` |
| Page component with a store | `createTestingPinia` with `initialState` |
| Composable with lifecycle hooks | `withSetup` helper wrapping `mount` |
| Async timing (debounce, polling) | `vi.useFakeTimers()` + `vi.advanceTimersByTimeAsync` |
| API function calls | `vi.mock` the feature's `api.ts` module |
| Error boundaries and error display | Mock API to throw, assert error element visible |
| Router navigation | `createRouter(createMemoryHistory())` in test global plugins |

## Execution

Run `find_vue_component` via vue-mcp to read the component under test before writing specs. Run `get_vue_type_errors` after writing tests to confirm types are correct.

### `/testing [component or feature to test]`

1. Identify the type of thing to test (component, composable, store, page).
2. Read the source file to understand props, emits, state, and actions.
3. Generate a `.spec.ts` file in the same directory with:
   - Grouped `describe` blocks per scenario
   - `afterEach(() => wrapper.unmount())` for component tests
   - `beforeEach(() => setActivePinia(createPinia()))` and `vi.mock` for store tests
   - AAA pattern in each `it` block
   - No real network calls
4. Cover: happy path, error state, loading state, edge cases (empty, boundary values).
5. Run `get_vue_type_errors` to validate.

$ARGUMENTS
