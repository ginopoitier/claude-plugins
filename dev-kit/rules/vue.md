# Rule: Vue 3 Conventions

## DO
- Always use `<script setup lang="ts">` — no Options API, no `defineComponent`
- Type props with `defineProps<{ prop: Type }>()` — no runtime validators
- Type emits with `defineEmits<{ eventName: [arg: Type] }>()`
- Use **Pinia** for all shared state — no Vuex, no provide/inject for global state
- Define stores with the Composition API style (`defineStore('name', () => { ... })`)
- Manage **SignalR** connections in a dedicated Pinia store (`signalrStore`) — not in components
- Call **cleanup** in `onUnmounted`: remove SignalR listeners, cancel subscriptions
- Put API calls in `features/{name}/api.ts` — never raw `fetch`/`axios` in components
- Use `@/` alias for imports, not relative paths like `../../`
- Co-locate component logic: feature components live in `features/{name}/components/`

## DON'T
- Don't use `<script>` without `setup` — always `<script setup>`
- Don't call APIs directly from component `<script setup>` — go through the api layer or store
- Don't register SignalR `.on()` listeners without a corresponding `.off()` in `onUnmounted`
- Don't use inline styles — TailwindCSS classes only
- Don't create global CSS files for component styling — use Tailwind utilities
- Don't use `defineComponent` wrappers — `<script setup>` is sufficient

## Example

```vue
<!-- GOOD — script setup, typed props/emits, store for state, @/ alias -->
<script setup lang="ts">
import { useOrderStore } from '@/features/orders/stores/orderStore'
const props = defineProps<{ orderId: string }>()
const emit = defineEmits<{ cancelled: [orderId: string] }>()
const store = useOrderStore()
async function handleCancel() {
  await store.cancelOrder(props.orderId)
  emit('cancelled', props.orderId)
}
</script>

<!-- BAD — Options API, no types, inline API call, relative import -->
<script>
import axios from 'axios'
import OrderStore from '../../stores/OrderStore' // relative path
export default {
  props: ['orderId'],               // no types
  methods: {
    async cancel() {
      await axios.delete(`/api/orders/${this.orderId}`) // API call in component
    }
  }
}
</script>
```

## Deep Reference
For full patterns, SignalR store, and Vite config: @~/.claude/knowledge/vue/vue-patterns.md
