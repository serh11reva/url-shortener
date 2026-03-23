<script setup lang="ts">
import { ref } from 'vue';
import ClayField from '@/components/ClayField.vue';
import ClayButton from '@/components/ClayButton.vue';

withDefaults(
  defineProps<{
    disabled?: boolean;
  }>(),
  { disabled: false },
);

const longUrl = ref('');
const alias = ref('');

const emit = defineEmits<{
  submit: [payload: { longUrl: string; alias: string | undefined }];
}>();

function onSubmit() {
  emit('submit', {
    longUrl: longUrl.value.trim(),
    alias: alias.value.trim() || undefined,
  });
}

defineExpose({
  reset() {
    longUrl.value = '';
    alias.value = '';
  },
});
</script>

<template>
  <form class="form" @submit.prevent="onSubmit">
    <ClayField
      id="long-url"
      label="Long URL"
      hint="Must start with http:// or https://"
    >
      <input
        id="long-url"
        v-model="longUrl"
        type="url"
        name="longUrl"
        required
        autocomplete="url"
        :disabled="disabled"
        placeholder="https://example.com/very/long/path"
      />
    </ClayField>
    <ClayField
      id="alias"
      label="Custom alias"
      hint="Optional. Letters and numbers, up to 7 characters."
    >
      <input
        id="alias"
        v-model="alias"
        type="text"
        name="alias"
        maxlength="7"
        pattern="[a-zA-Z0-9]*"
        inputmode="text"
        autocomplete="off"
        :disabled="disabled"
        placeholder="myLink"
      />
    </ClayField>
    <ClayButton type="submit" class="submit" :disabled="disabled">Shorten</ClayButton>
  </form>
</template>

<style scoped>
.form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.submit {
  margin-top: 0.25rem;
  align-self: stretch;
}
</style>
