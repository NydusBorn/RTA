<script setup lang="ts">
import mermaid from 'mermaid';
import {useFileDialog} from "@vueuse/core";
mermaid.initialize({ startOnLoad: true });
const backend_host = ref("");
if (process.client){
  onMounted(() => {
    nextTick(async () => {
      await useFetch("/api/get_backend_host").then(x => backend_host.value = x.data.value)
    })
  })
}
const input_regex = ref('');
const output_NFA = ref('');
const load_regex = async () => {
  let dialog = useFileDialog()
  dialog.onChange((files) => {
    let reader = new FileReader()
    reader.onload = () => {
      input_regex.value = reader.result as string
    }
    reader.readAsText(files[0])
  })
  dialog.open()
}

const send_request = async () => {
  await fetch(`http://${backend_host.value.host}/api`, {
    method: 'POST',
    body: JSON.stringify({regex: input_regex.value}),
  }).then(x => x.text().then(xy => output_NFA.value = xy))
}
</script>

<template>
  <div style="display: flex; flex-direction: row; gap: 1rem">
    <div style="display: flex; flex-direction: column; gap: 1rem">
      <el-input v-model="input_regex" @change="send_request" placeholder="Enter regex" :autosize="{minRows: 5, maxRows:10}" type="textarea" style="width: 400px;"/>
      <el-button @click="load_regex">Load from file</el-button>
    </div>
    <div style="display: flex; flex-direction: column; gap: 1rem">
      <el-input readonly v-model="output_NFA" placeholder="NFA" :autosize="{minRows: 5, maxRows:10}" type="textarea" style="width: 400px;"/>
      <el-button>Save to file</el-button>
      <pre class="mermaid">graph LR<br/>A --- B<br/>B-->C[fa:fa-ban forbidden]<br/>B-->D(fa:fa-spinner);</pre>
      <el-table stripe>

      </el-table>
    </div>

  </div>
</template>

<style>
:root {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 100vh;
  width: 100vw;
}
</style>
