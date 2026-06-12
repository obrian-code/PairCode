let updateTimeout;
let editor;

const editorConnection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/document")
  .build();

const modeMap = {
  javascript: "javascript",
  python: "python",
  csharp: "text/x-csharp",
  html: "htmlmixed",
  css: "css"
};

function initCodeMirror() {
  const textarea = document.getElementById("editorContent");
  if (!textarea) return;

  editor = CodeMirror.fromTextArea(textarea, {
    lineNumbers: true,
    theme: "dracula",
    mode: "javascript",
    indentUnit: 2,
    tabSize: 2,
    lineWrapping: true,
    autofocus: false
  });

  editor.on("change", () => {
    clearTimeout(updateTimeout);
    updateTimeout = setTimeout(() => {
      const content = editor.getValue();
      editorConnection.invoke("SendUpdate", roomId, content).catch(() => {});
    }, 300);
  });

  document.getElementById("languageSelect")?.addEventListener("change", (e) => {
    const mode = modeMap[e.target.value] || "javascript";
    editor.setOption("mode", mode);
    const display = document.querySelector("#editor .version-badge");
    if (display) {
      display.textContent = `Mode: ${e.target.value} | Version ${version || 1}`;
    }
  });
}

let version = 1;

editorConnection.on("DocumentUpdated", (document) => {
  if (editor) {
    const cursor = editor.getCursor();
    editor.setValue(document.content);
    editor.setCursor(cursor);
    version = document.version;
    updateVersionDisplay(document.version, document.updatedAt);
  }
});

editorConnection.on("DocumentLoaded", (document) => {
  if (editor) {
    editor.setValue(document.content);
    version = document.version;
    updateVersionDisplay(document.version, document.updatedAt);
  }
});

editorConnection.start().then(() => {
  editorConnection.invoke("JoinDocument", roomId);
  editorConnection.invoke("RequestDocument", roomId);
  initCodeMirror();
});

function updateVersionDisplay(version, updatedAt) {
  const display = document.querySelector("#editor .version-badge");
  if (display) {
    display.textContent = `Version ${version} | Updated: ${new Date(updatedAt).toLocaleString()}`;
  }
}
