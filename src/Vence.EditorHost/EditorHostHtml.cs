using System.Text.Json;

namespace Vence.EditorHost;

internal static class EditorHostHtml
{
    public static string Create(string markdown)
    {
        var encodedMarkdown = JsonSerializer.Serialize(markdown, EditorJsonSerializerContext.Default.String);

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

                    html,
                    body {
                        margin: 0;
                        width: 100%;
                        min-height: 100%;
                        background: #fffdf8;
                    }

                    main {
                        box-sizing: border-box;
                        min-height: 100vh;
                        padding: 34px 38px;
                    }

                    .editor {
                        box-sizing: border-box;
                        width: 100%;
                        min-height: calc(100vh - 68px);
                        border: 0;
                        outline: none;
                        resize: none;
                        overflow: hidden;
                        color: #252a26;
                        background: transparent;
                        font: 17px/1.85 "Microsoft YaHei UI", "Segoe UI", sans-serif;
                        white-space: pre-wrap;
                    }

                    .editor::selection,
                    .reader-view ::selection {
                        background: #dce8e4;
                    }

                    .reader-view {
                        max-width: 760px;
                        margin: 0 auto;
                        color: #252a26;
                        font-size: 17px;
                        line-height: 1.9;
                    }

                    .reader-view [data-outline-index] {
                        scroll-margin-top: 24px;
                    }

                    .reader-view h1,
                    .reader-view h2,
                    .reader-view h3 {
                        margin: 1.35em 0 0.55em;
                        font-family: Georgia, "Source Han Serif SC", serif;
                        line-height: 1.35;
                    }

                    .reader-view h1 {
                        margin-top: 0;
                        font-size: 34px;
                    }

                    .reader-view h2 {
                        font-size: 25px;
                    }

                    .reader-view h3 {
                        font-size: 20px;
                    }

                    .reader-view p {
                        margin: 0.7em 0;
                    }

                    .reader-view ul,
                    .reader-view ol {
                        margin: 0.7em 0 0.9em 1.35em;
                        padding: 0;
                    }

                    .reader-view blockquote {
                        margin: 1em 0;
                        padding: 0.2em 0 0.2em 1em;
                        border-left: 3px solid #b7c8c1;
                        color: #526159;
                    }

                    .reader-view code {
                        padding: 0.12em 0.35em;
                        border-radius: 4px;
                        background: #eee7dc;
                        font-family: "Cascadia Code", Consolas, monospace;
                        font-size: 0.92em;
                    }

                    .reader-view pre {
                        overflow-x: auto;
                        padding: 14px 16px;
                        border: 1px solid #e3dacb;
                        border-radius: 8px;
                        background: #f7f0e6;
                    }

                    .reader-view pre code {
                        padding: 0;
                        background: transparent;
                    }

                    .mermaid-block {
                        overflow-x: auto;
                        margin: 1.2em 0;
                        padding: 16px;
                        border: 1px solid #e3dacb;
                        border-radius: 8px;
                        background: #fffaf2;
                        text-align: center;
                    }

                    .mermaid-block pre {
                        margin: 0;
                        text-align: left;
                    }

                    .mermaid-block svg {
                        max-width: 100%;
                        height: auto;
                    }

                    .math-display {
                        overflow-x: auto;
                        margin: 1em 0;
                    }

                    .math-inline {
                        white-space: nowrap;
                    }

                    .render-error-text {
                        margin-bottom: 8px;
                        color: #9c3f2e;
                        font-size: 13px;
                        text-align: left;
                    }

                    .reader-view table {
                        width: 100%;
                        margin: 1em 0;
                        border-collapse: collapse;
                    }

                    .reader-view th,
                    .reader-view td {
                        padding: 8px 10px;
                        border: 1px solid #e3dacb;
                        text-align: left;
                    }

                    .reader-view img {
                        max-width: 100%;
                        border-radius: 8px;
                    }
                </style>
            </head>
            <body>
                <main>
                    <textarea id="editor" class="editor" spellcheck="false"></textarea>
                    <article id="reader" class="reader-view" hidden></article>
                </main>
                <script>
                    const initialMarkdown = {{encodedMarkdown}};
                    const editor = document.getElementById("editor");
                    const reader = document.getElementById("reader");
                    let isReaderMode = false;
                    let pendingMermaidBlocks = [];
                    let mermaidModulePromise = null;
                    let mathJaxPromise = null;

                    editor.value = initialMarkdown;
                    resizeEditor();

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

                    function currentPayload() {
                        return {
                            markdown: editor.value,
                            characterCount: editor.value.length
                        };
                    }

                    function postChanged() {
                        resizeEditor();
                        if (isReaderMode) {
                            renderReader();
                        }
                        post("document.changed", currentPayload());
                    }

                    function resizeEditor() {
                        editor.style.height = "auto";
                        editor.style.height = `${Math.max(editor.scrollHeight, window.innerHeight - 68)}px`;
                    }

                    function escapeHtml(value) {
                        return value
                            .replaceAll("&", "&amp;")
                            .replaceAll("<", "&lt;")
                            .replaceAll(">", "&gt;")
                            .replaceAll('"', "&quot;");
                    }

                    function takeInlineToken(tokens, html) {
                        const token = `@@VENCE_INLINE_${tokens.length}@@`;
                        tokens.push({ token, html });
                        return token;
                    }

                    function renderInlineMath(formula) {
                        return `<span class="math-inline">\\(${escapeHtml(formula.trim())}\\)</span>`;
                    }

                    function renderDisplayMath(formula) {
                        return `<div class="math-display">\\[${escapeHtml(formula.trim())}\\]</div>`;
                    }

                    function renderInline(value) {
                        const tokens = [];
                        let source = value.replace(/`([^`]+)`/g, (_, code) => takeInlineToken(tokens, `<code>${escapeHtml(code)}</code>`));
                        source = source.replace(/\$([^$\n]+?)\$/g, (_, formula) => takeInlineToken(tokens, renderInlineMath(formula)));

                        let html = escapeHtml(source);
                        html = html.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, '<img alt="$1" src="$2" />');
                        html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');
                        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
                        html = html.replace(/\*([^*]+)\*/g, '<em>$1</em>');
                        for (const token of tokens) {
                            html = html.replaceAll(token.token, token.html);
                        }
                        return html;
                    }

                    function renderTable(lines, startIndex) {
                        if (startIndex + 1 >= lines.length || !/^\s*\|?[-:| ]+\|[-:| ]+\|?\s*$/.test(lines[startIndex + 1])) {
                            return null;
                        }

                        const rows = [];
                        let index = startIndex;
                        while (index < lines.length && lines[index].includes("|") && lines[index].trim() !== "") {
                            if (index !== startIndex + 1) {
                                rows.push(lines[index]);
                            }
                            index++;
                        }

                        const cells = row => row.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map(cell => renderInline(cell.trim()));
                        const head = cells(rows[0]);
                        const body = rows.slice(1).map(row => cells(row));
                        const headHtml = `<thead><tr>${head.map(cell => `<th>${cell}</th>`).join("")}</tr></thead>`;
                        const bodyHtml = `<tbody>${body.map(row => `<tr>${row.map(cell => `<td>${cell}</td>`).join("")}</tr>`).join("")}</tbody>`;

                        return {
                            html: `<table>${headHtml}${bodyHtml}</table>`,
                            nextIndex: index
                        };
                    }

                    function renderMarkdown(markdown) {
                        pendingMermaidBlocks = [];
                        const lines = markdown.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
                        const html = [];
                        let index = 0;
                        let headingIndex = 0;

                        while (index < lines.length) {
                            const line = lines[index];
                            const trimmed = line.trim();

                            if (trimmed.length === 0) {
                                index++;
                                continue;
                            }

                            if (trimmed.startsWith("```")) {
                                const language = trimmed.slice(3).trim();
                                const normalizedLanguage = language.split(/\s+/)[0].toLowerCase();
                                const codeLines = [];
                                index++;
                                while (index < lines.length && !lines[index].trim().startsWith("```")) {
                                    codeLines.push(lines[index]);
                                    index++;
                                }
                                index++;

                                const code = codeLines.join("\n");
                                if (normalizedLanguage === "mermaid") {
                                    const diagramIndex = pendingMermaidBlocks.push(code) - 1;
                                    html.push(`<div class="mermaid-block" data-mermaid-index="${diagramIndex}"><pre><code data-language="mermaid">${escapeHtml(code)}</code></pre></div>`);
                                    continue;
                                }

                                if (normalizedLanguage === "math" || normalizedLanguage === "latex" || normalizedLanguage === "tex") {
                                    html.push(renderDisplayMath(code));
                                    continue;
                                }

                                html.push(`<pre><code data-language="${escapeHtml(language)}">${escapeHtml(code)}</code></pre>`);
                                continue;
                            }

                            if (trimmed.startsWith("$$")) {
                                const mathLines = [];
                                let firstLine = trimmed.slice(2);
                                const singleLine = firstLine.trimEnd();
                                if (singleLine.endsWith("$$") && singleLine.length > 2) {
                                    mathLines.push(singleLine.slice(0, -2));
                                    index++;
                                } else {
                                    if (firstLine.trim().length > 0) {
                                        mathLines.push(firstLine);
                                    }
                                    index++;
                                    while (index < lines.length && !lines[index].trim().endsWith("$$")) {
                                        mathLines.push(lines[index]);
                                        index++;
                                    }

                                    if (index < lines.length) {
                                        const closingLine = lines[index];
                                        mathLines.push(closingLine.slice(0, closingLine.lastIndexOf("$$")));
                                        index++;
                                    }
                                }

                                html.push(renderDisplayMath(mathLines.join("\n")));
                                continue;
                            }

                            const heading = /^(#{1,6})\s+(.+)$/.exec(trimmed);
                            if (heading) {
                                const level = heading[1].length;
                                html.push(`<h${level} data-outline-index="${headingIndex}">${renderInline(heading[2])}</h${level}>`);
                                headingIndex++;
                                index++;
                                continue;
                            }

                            if (/^---+$/.test(trimmed)) {
                                html.push("<hr />");
                                index++;
                                continue;
                            }

                            const table = renderTable(lines, index);
                            if (table) {
                                html.push(table.html);
                                index = table.nextIndex;
                                continue;
                            }

                            if (/^>\s?/.test(trimmed)) {
                                const quoteLines = [];
                                while (index < lines.length && /^>\s?/.test(lines[index].trim())) {
                                    quoteLines.push(lines[index].trim().replace(/^>\s?/, ""));
                                    index++;
                                }
                                html.push(`<blockquote>${quoteLines.map(renderInline).map(item => `<p>${item}</p>`).join("")}</blockquote>`);
                                continue;
                            }

                            if (/^[-*+]\s+/.test(trimmed)) {
                                const items = [];
                                while (index < lines.length && /^[-*+]\s+/.test(lines[index].trim())) {
                                    items.push(lines[index].trim().replace(/^[-*+]\s+/, ""));
                                    index++;
                                }
                                html.push(`<ul>${items.map(item => `<li>${renderInline(item)}</li>`).join("")}</ul>`);
                                continue;
                            }

                            if (/^\d+\.\s+/.test(trimmed)) {
                                const items = [];
                                while (index < lines.length && /^\d+\.\s+/.test(lines[index].trim())) {
                                    items.push(lines[index].trim().replace(/^\d+\.\s+/, ""));
                                    index++;
                                }
                                html.push(`<ol>${items.map(item => `<li>${renderInline(item)}</li>`).join("")}</ol>`);
                                continue;
                            }

                            const paragraph = [trimmed];
                            index++;
                            while (index < lines.length && lines[index].trim() !== "") {
                                const next = lines[index].trim();
                                if (/^(#{1,6})\s+/.test(next) || /^[-*+]\s+/.test(next) || /^\d+\.\s+/.test(next) || /^>\s?/.test(next) || next.startsWith("```") || next.startsWith("$$")) {
                                    break;
                                }
                                paragraph.push(next);
                                index++;
                            }

                            html.push(`<p>${renderInline(paragraph.join(" "))}</p>`);
                        }

                        return html.join("\n");
                    }

                    function loadMermaid() {
                        if (mermaidModulePromise) {
                            return mermaidModulePromise;
                        }

                        mermaidModulePromise = import("https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs")
                            .then(module => {
                                const mermaid = module.default;
                                mermaid.initialize({
                                    startOnLoad: false,
                                    securityLevel: "strict",
                                    theme: "neutral"
                                });
                                return mermaid;
                            })
                            .catch(() => null);

                        return mermaidModulePromise;
                    }

                    function loadMathJax() {
                        if (window.MathJax?.typesetPromise) {
                            return Promise.resolve(window.MathJax);
                        }

                        if (mathJaxPromise) {
                            return mathJaxPromise;
                        }

                        window.MathJax = {
                            tex: {
                                inlineMath: [["\\(", "\\)"]],
                                displayMath: [["\\[", "\\]"]],
                                processEscapes: true
                            },
                            svg: {
                                fontCache: "global"
                            },
                            startup: {
                                typeset: false
                            }
                        };

                        mathJaxPromise = new Promise(resolve => {
                            const script = document.createElement("script");
                            script.src = "https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-svg.js";
                            script.async = true;
                            script.onload = () => resolve(window.MathJax ?? null);
                            script.onerror = () => resolve(null);
                            document.head.appendChild(script);
                        });

                        return mathJaxPromise;
                    }

                    async function renderMermaidBlocks() {
                        const blocks = Array.from(reader.querySelectorAll(".mermaid-block"));
                        if (blocks.length === 0) {
                            return;
                        }

                        const mermaidSources = pendingMermaidBlocks.slice();
                        const mermaid = await loadMermaid();
                        if (!mermaid) {
                            blocks.forEach(block => block.classList.add("render-error"));
                            return;
                        }

                        for (const [blockIndex, block] of blocks.entries()) {
                            const sourceIndex = Number(block.dataset.mermaidIndex);
                            const source = mermaidSources[sourceIndex] ?? "";
                            if (!source.trim()) {
                                continue;
                            }

                            try {
                                const renderId = `vence-mermaid-${Date.now()}-${blockIndex}`;
                                const result = await mermaid.render(renderId, source);
                                block.innerHTML = result.svg;
                                result.bindFunctions?.(block);
                            } catch {
                                block.classList.add("render-error");
                                block.insertAdjacentHTML("afterbegin", '<div class="render-error-text">Mermaid 渲染失败，已保留源代码。</div>');
                            }
                        }
                    }

                    async function renderMathBlocks() {
                        if (!reader.querySelector(".math-inline, .math-display")) {
                            return;
                        }

                        const mathJax = await loadMathJax();
                        if (!mathJax?.typesetPromise) {
                            return;
                        }

                        await mathJax.typesetPromise([reader]);
                    }

                    async function renderEnhancements() {
                        await Promise.all([
                            renderMermaidBlocks(),
                            renderMathBlocks()
                        ]);
                    }

                    function renderReader() {
                        reader.innerHTML = renderMarkdown(editor.value);
                        void renderEnhancements();
                    }

                    function findHeadingOffset(headingIndex) {
                        const lines = editor.value.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
                        let offset = 0;
                        let currentHeadingIndex = 0;

                        for (const line of lines) {
                            if (/^\s*#{1,6}\s+/.test(line)) {
                                if (currentHeadingIndex === headingIndex) {
                                    return offset + Math.max(0, line.search(/\S/));
                                }

                                currentHeadingIndex++;
                            }

                            offset += line.length + 1;
                        }

                        return -1;
                    }

                    function scrollEditorToOffset(offset) {
                        const before = editor.value.slice(0, offset);
                        const lineCount = before.split("\n").length - 1;
                        const lineHeight = Number.parseFloat(getComputedStyle(editor).lineHeight) || 30;
                        const editorTop = editor.getBoundingClientRect().top + window.scrollY;
                        window.scrollTo({
                            top: Math.max(0, editorTop + lineCount * lineHeight - 24),
                            behavior: "smooth"
                        });
                    }

                    function scrollToHeading(headingIndex) {
                        const targetIndex = Number(headingIndex);
                        if (!Number.isInteger(targetIndex) || targetIndex < 0) {
                            return;
                        }

                        if (isReaderMode) {
                            const heading = reader.querySelector(`[data-outline-index="${targetIndex}"]`);
                            heading?.scrollIntoView({
                                behavior: "smooth",
                                block: "start"
                            });
                            return;
                        }

                        const offset = findHeadingOffset(targetIndex);
                        if (offset < 0) {
                            return;
                        }

                        editor.focus();
                        editor.selectionStart = offset;
                        editor.selectionEnd = offset;
                        scrollEditorToOffset(offset);
                    }

                    function setReaderMode(enabled) {
                        isReaderMode = enabled;
                        if (enabled) {
                            renderReader();
                            editor.hidden = true;
                            reader.hidden = false;
                            postSelection();
                        } else {
                            reader.hidden = true;
                            editor.hidden = false;
                            resizeEditor();
                            editor.focus();
                        }
                    }

                    function postSelection() {
                        if (!isReaderMode) {
                            return;
                        }

                        const selectedText = window.getSelection()?.toString() ?? "";
                        post("reader.selectionChanged", {
                            selectedText,
                            selectedLength: selectedText.length
                        });
                    }

                    function replaceSelection(replacement, selectStartOffset = replacement.length, selectEndOffset = replacement.length) {
                        const start = editor.selectionStart;
                        const end = editor.selectionEnd;
                        editor.setRangeText(replacement, start, end, "end");
                        editor.focus();
                        editor.selectionStart = start + selectStartOffset;
                        editor.selectionEnd = start + selectEndOffset;
                        postChanged();
                    }

                    function wrapSelection(before, after, placeholder) {
                        const start = editor.selectionStart;
                        const end = editor.selectionEnd;
                        const selected = editor.value.slice(start, end) || placeholder;
                        const replacement = `${before}${selected}${after}`;
                        editor.setRangeText(replacement, start, end, "end");
                        editor.focus();
                        editor.selectionStart = start + before.length;
                        editor.selectionEnd = start + before.length + selected.length;
                        postChanged();
                    }

                    function applyMarkdownCommand(command) {
                        if (isReaderMode) {
                            return;
                        }

                        switch (command) {
                            case "bold":
                                wrapSelection("**", "**", "加粗文本");
                                break;
                            case "italic":
                                wrapSelection("*", "*", "斜体文本");
                                break;
                            case "link":
                                wrapSelection("[", "](https://)", "链接文本");
                                break;
                            case "image":
                                replaceSelection("![图片描述](image-path)", 2, 6);
                                break;
                        }
                    }

                    editor.addEventListener("input", postChanged);

                    editor.addEventListener("keydown", event => {
                        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") {
                            event.preventDefault();
                            event.stopPropagation();
                            post("document.saveRequested", currentPayload());
                            return;
                        }

                        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "b") {
                            event.preventDefault();
                            event.stopPropagation();
                            applyMarkdownCommand("bold");
                            return;
                        }

                        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "i") {
                            event.preventDefault();
                            event.stopPropagation();
                            applyMarkdownCommand("italic");
                            return;
                        }

                        if (event.key === "Tab") {
                            event.preventDefault();
                            event.stopPropagation();
                            replaceSelection("  ");
                        }
                    });

                    document.addEventListener("keydown", event => {
                        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") {
                            event.preventDefault();
                            post("document.saveRequested", currentPayload());
                        }
                    });

                    document.addEventListener("selectionchange", postSelection);
                    window.addEventListener("resize", resizeEditor);

                    window.chrome?.webview?.addEventListener("message", event => {
                        if (!event.data) {
                            return;
                        }

                        const message = JSON.parse(event.data);
                        if (message.type === "document.setMarkdown") {
                            editor.value = message.payload.markdown ?? "";
                            if (isReaderMode) {
                                renderReader();
                            }
                            resizeEditor();
                        }

                        if (message.type === "document.setReaderMode") {
                            setReaderMode(Boolean(message.payload.enabled));
                        }

                        if (message.type === "document.scrollToHeading") {
                            scrollToHeading(message.payload.headingIndex);
                        }

                        if (message.type === "editor.applyMarkdownCommand") {
                            applyMarkdownCommand(message.payload.command);
                        }
                    });

                    post("editor.ready", currentPayload());
                </script>
            </body>
            </html>
            """;
    }
}
