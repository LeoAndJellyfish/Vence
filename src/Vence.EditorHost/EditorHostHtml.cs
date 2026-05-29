using System.Text.Json;

namespace Vence.EditorHost;

internal static class EditorHostHtml
{
    public static string Create(string markdown)
    {
        var encodedMarkdown = JsonSerializer.Serialize(markdown);

        return $$"""
            <!doctype html>
            <html lang="zh-CN">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <style>
                    :root {
                        color: #252a26;
                        background: #fffdf8;
                        font-family: "Microsoft YaHei UI", "Segoe UI", sans-serif;
                    }

                    body {
                        margin: 0;
                        min-height: 100vh;
                        background: #fffdf8;
                    }

                    main {
                        box-sizing: border-box;
                        min-height: 100vh;
                        padding: 34px 38px;
                    }

                    h1 {
                        margin: 0 0 22px;
                        font-family: Georgia, "Source Han Serif SC", serif;
                        font-size: 30px;
                        line-height: 1.35;
                        font-weight: 600;
                    }

                    .editor {
                        outline: none;
                        min-height: 420px;
                        font-size: 17px;
                        line-height: 1.85;
                        white-space: pre-wrap;
                    }
                </style>
            </head>
            <body>
                <main>
                    <h1>探索山城 Vence 的艺术与生活之美</h1>
                    <section id="editor" class="editor" contenteditable="true" spellcheck="false"></section>
                </main>
                <script>
                    const initialMarkdown = {{encodedMarkdown}};
                    const editor = document.getElementById("editor");
                    editor.textContent = initialMarkdown;

                    function post(type, payload) {
                        const message = {
                            version: 1,
                            type,
                            requestId: crypto.randomUUID ? crypto.randomUUID().replaceAll("-", "") : String(Date.now()),
                            payload
                        };

                        if (window.chrome && window.chrome.webview) {
                            window.chrome.webview.postMessage(JSON.stringify(message));
                        }
                    }

                    editor.addEventListener("input", () => {
                        post("document.changed", {
                            markdown: editor.textContent,
                            characterCount: editor.textContent.length
                        });
                    });

                    window.chrome?.webview?.addEventListener("message", event => {
                        if (!event.data) {
                            return;
                        }

                        const message = JSON.parse(event.data);
                        if (message.type === "document.setMarkdown") {
                            editor.textContent = message.payload.markdown ?? "";
                        }
                    });

                    post("editor.ready", { markdown: editor.textContent });
                </script>
            </body>
            </html>
            """;
    }
}
