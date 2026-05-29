import { postToHost } from "./bridge";

const editor = document.querySelector<HTMLElement>("[data-editor]");

if (editor) {
  editor.addEventListener("input", () => {
    postToHost("document.changed", {
      markdown: editor.textContent ?? "",
      characterCount: editor.textContent?.length ?? 0,
    });
  });

  postToHost("editor.ready", {
    markdown: editor.textContent ?? "",
  });
}
