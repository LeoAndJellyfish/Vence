export type EditorMessage<TPayload = unknown> = {
  version: 1;
  type: string;
  requestId: string;
  payload: TPayload;
};

export function createMessage<TPayload>(
  type: string,
  payload: TPayload,
): EditorMessage<TPayload> {
  return {
    version: 1,
    type,
    requestId: crypto.randomUUID().replaceAll("-", ""),
    payload,
  };
}

export function postToHost<TPayload>(type: string, payload: TPayload): void {
  window.chrome?.webview?.postMessage(JSON.stringify(createMessage(type, payload)));
}
