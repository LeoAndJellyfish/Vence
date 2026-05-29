type WebView2MessageHandler = (event: MessageEvent<unknown>) => void;

interface WebView2Bridge {
  postMessage(message: string): void;
  addEventListener(type: "message", handler: WebView2MessageHandler): void;
}

interface Window {
  chrome?: {
    webview?: WebView2Bridge;
  };
}
