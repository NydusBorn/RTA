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
  let dialog = useFileDialog({
    accept: ".txt",
  })
  dialog.onChange((files) => {
    let reader = new FileReader()
    reader.onload = () => {
      input_regex.value = reader.result as string
    }
    reader.readAsText(files[0])
    send_request()
  })
  dialog.open()
}

const save_file = async () => {
  let dialog = await showSaveFilePicker({
    suggestedName: 'nfa.txt',
    types: [{
      description: 'Text file',
      accept: {'text/plain': ['.txt']},
    }],
  })
  let writer = await dialog.createWritable()
  writer.write(output_NFA.value)
  writer.close()
}

const send_request = async () => {
  await fetch(`${backend_host.value.host}/api`, {
    method: 'POST',
    body: JSON.stringify(input_regex.value),
  }).then(x => x.text().then(xy => {
    output_NFA.value = xy
    mermaid.run()
  }))
}
</script>

<template>
  <div style="display: flex; flex-direction: row; gap: 1rem">
    <div style="display: flex; flex-direction: column; gap: 1rem">
      <el-input v-model="input_regex" @input="send_request" placeholder="Enter regex" :autosize="{minRows: 5, maxRows:10}" type="textarea" style="width: 400px;"/>
      <el-button @click="load_regex">Load from file</el-button>
    </div>
    <div style="display: flex; flex-direction: column; gap: 1rem">
      <el-input readonly v-model="output_NFA" placeholder="NFA" :autosize="{minRows: 5, maxRows:10}" type="textarea" style="width: 400px;"/>
      <el-button @click="save_file">Save to file</el-button>
      <pre class="mermaid">{{output_NFA}}</pre>
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
