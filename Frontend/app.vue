<script>
import mermaid from 'mermaid';
mermaid.initialize({ startOnLoad: true });
</script>

<template>
  <div>
    <NuxtWelcome />
    <pre class="mermaid">
      graph LR
      A --- B
      B-->C[fa:fa-ban forbidden]
      B-->D(fa:fa-spinner);
    </pre>
  </div>
</template>

<style>

</style>
