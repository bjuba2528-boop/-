// Bridge для совместимости с Tauri API
// Этот файл позволяет использовать существующий код без изменений

interface WebViewMessage {
  command: string;
  data?: any;
}

interface WebViewResponse {
  event: string;
  data: any;
}

class TauriBridge {
  private messageHandlers: Map<string, (data: any) => void> = new Map();

  constructor() {
    // Слушаем сообщения от C#
    window.addEventListener('message', (event: MessageEvent<WebViewResponse>) => {
      const { event: eventName, data } = event.data;
      const handler = this.messageHandlers.get(eventName);
      if (handler) {
        handler(data);
        this.messageHandlers.delete(eventName);
      }
    });
  }

  // Эмуляция Tauri invoke
  async invoke<T = any>(command: string, args?: any): Promise<T> {
    return new Promise((resolve, reject) => {
      const responseEvent = `${command}_response`;
      const timeoutId = setTimeout(() => {
        this.messageHandlers.delete(responseEvent);
        reject(new Error(`Timeout: команда ${command} не ответила за 10 секунд`));
      }, 10000);

      this.messageHandlers.set(responseEvent, (data: T) => {
        clearTimeout(timeoutId);
        resolve(data);
      });

      // Отправляем команду в C#
      if (window.chrome?.webview) {
        window.chrome.webview.postMessage({
          command,
          data: args
        } as WebViewMessage);
      } else {
        clearTimeout(timeoutId);
        this.messageHandlers.delete(responseEvent);
        reject(new Error('WebView2 недоступен'));
      }
    });
  }

  // Эмуляция Tauri listen
  listen<T = any>(event: string, handler: (data: T) => void): () => void {
    const wrappedHandler = (e: MessageEvent<WebViewResponse>) => {
      if (e.data.event === event) {
        handler(e.data.data);
      }
    };

    window.addEventListener('message', wrappedHandler);

    // Возвращаем функцию отписки
    return () => {
      window.removeEventListener('message', wrappedHandler);
    };
  }
}

// Глобальный экземпляр
const bridge = new TauriBridge();

// Экспортируем как Tauri API
export const invoke = bridge.invoke.bind(bridge);
export const listen = bridge.listen.bind(bridge);

// Для прямого доступа
export default bridge;
