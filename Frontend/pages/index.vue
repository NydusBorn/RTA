<script setup lang="ts">
import mermaid from 'mermaid';
import {useFileDialog} from "@vueuse/core";

mermaid.initialize({theme: 'dark'});
const backend_host = ref("");
if (process.client) {
  onMounted(() => {
    nextTick(async () => {
      await useFetch("/api/get_backend_host").then(x => backend_host.value = x.data.value);
      const element = document.querySelector('#graph-container')!;
      const { svg, bindFunctions } = await mermaid.render('graph', output_graph.value, element);
      element.innerHTML = svg;
      bindFunctions?.(element);
    })
  })
}
const input_regex = ref('');
const output_NFA = ref('');
const output_table_columns = ref<string[]>([]);
const output_table_data = ref<object[]>([]);
const output_graph = ref('flowchart LR\n\ts((s))');
const load_regex = async () => {
  let dialog = useFileDialog({
    accept: ".txt",
  })
  dialog.onChange((files) => {
    let reader = new FileReader()
    reader.onload = () => {
      input_regex.value = reader.result as string
      send_request()
    }
    reader.readAsText(files[0])
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

const make_table = (x: string) => {
  output_table_columns.value = []
  output_table_data.value = []
  const lines = x.split('\n')
  let looking_at_char = false
  let headers = []
  for (let i = 0; i < lines[0].length; i++) {
    if(lines[0][i] === "'"){
      looking_at_char = true
    }
    else if (looking_at_char){
      headers.push(lines[0][i])
      looking_at_char = false
      i += 1
    }
  }
  output_table_columns.value = headers
  let entries: object[] = []
  for (let i = 1; i < lines.length; i++) {
    const line = lines[i].split('\t')
    let entry = {}
    entry["state"] = line[0]
    for (let j = 0; j < headers.length; j++) {
      entry[headers[j]] = line[j + 1]
    }
    entries.push(entry)
  }
  output_table_data.value = entries
}

const send_request = async () => {
  await fetch(`${backend_host.value.host}/api`, {
    method: 'POST',
    body: JSON.stringify(input_regex.value),
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    }
  }).then(x => {
    x.json().then(xy => {
      output_NFA.value = xy.item1
      make_table(xy.item1)
      output_graph.value = xy.item2
      const element = document.querySelector('#graph-container')!;
      mermaid.render('graph', output_graph.value, element).then(x => {
        element.innerHTML = x.svg;
        x.bindFunctions?.(element);
      });
    })
  })
}
</script>

<template>
  <div style="display: flex; flex-direction: column; gap: 0.5rem; justify-content: center; align-items: center;">
    <div style="display: flex; flex-direction: row; gap: 1rem">
      <div style="display: flex; flex-direction: column; gap: 1rem">
        <el-input v-model="input_regex" @input="send_request" placeholder="Enter regex"
                  :autosize="{minRows: 5, maxRows:10}" type="textarea" style="width: 400px;"/>
        <el-button @click="load_regex">Load from file</el-button>
      </div>
      <div style="display: flex; flex-direction: column; gap: 1rem">
        <el-input readonly v-model="output_NFA" placeholder="NFA" :autosize="{minRows: 5, maxRows:10}" type="textarea"
                  style="width: 400px;"/>
        <el-button @click="save_file">Save to file</el-button>
      </div>
    </div>
    <div id="graph-container" class="mermaid" style="overflow: scroll; width: 100%; height: 600px">
      <svg id="graph"/>
    </div>
    <el-table stripe :data="output_table_data" style="width: 100vw;">
      <el-table-column prop="state" label="state"></el-table-column>
      <el-table-column v-for="x in output_table_columns" :key="x" :prop="x" :label="x"/>
    </el-table>
  </div>
</template>

<style>

</style>
