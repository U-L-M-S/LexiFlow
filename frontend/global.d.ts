declare class Worker {
  constructor(stringUrl: string | URL, options?: Record<string, unknown>);
  postMessage(message: unknown, transfer?: any[]): void;
  terminate(): void;
  onmessage: ((this: Worker, ev: MessageEvent) => unknown) | null;
  onmessageerror: ((this: Worker, ev: MessageEvent) => unknown) | null;
}
