/**
 * HN212 Smart Reader Service Integration (Zero-dependency Native WebSocket & REST)
 * Kết nối tới dịch vụ phần cứng đầu đọc CCCD HN212 (Hanel) qua http://localhost:5000
 */

export interface Hn212CardData {
  documentNumber?: string;
  fullName?: string;
  dateOfBirth?: string;
  sex?: string;
  nationality?: string;
  ethnicity?: string;
  religion?: string;
  hometown?: string;
  permanentAddress?: string;
  issueDate?: string;
  expiryDate?: string;
  chipFaceBase64?: string;
  readTime?: string;
  // Fallbacks for PascalCase from C# JSON serialization
  DocumentNumber?: string;
  FullName?: string;
  DateOfBirth?: string;
  Sex?: string;
  Nationality?: string;
  Ethnicity?: string;
  Religion?: string;
  Hometown?: string;
  PermanentAddress?: string;
  IssueDate?: string;
  ExpiryDate?: string;
  ChipFaceBase64?: string;
}

export interface Hn212ReaderStatus {
  isReaderConnected: boolean;
  readerSerialNumber: string;
  cardStatus: string;
  isCameraActive: boolean;
  message?: string;
  lastUpdated?: string;
}

export interface Hn212FaceCompareResult {
  score: number;
  isMatch: boolean;
  message: string;
  capturedFaceBase64?: string;
}

const RECORD_SEPARATOR = '\x1e';
const DEFAULT_HN212_HOST = 'http://localhost:5000';
const DEFAULT_HN212_WS = 'ws://localhost:5000/hubs/card';

/**
 * Chuyển đổi định dạng ngày tháng từ CCCD sang chuẩn yyyy-MM-dd cho input HTML type="date"
 * Hỗ trợ các định dạng: dd/MM/yyyy, dd-MM-yyyy, yyyyMMdd, yyyy-MM-dd
 */
export function parseCccdDate(raw?: string): string {
  if (!raw) return '';
  const clean = raw.trim();

  // dd/MM/yyyy
  if (/^\d{1,2}\/\d{1,2}\/\d{4}$/.test(clean)) {
    const [d, m, y] = clean.split('/');
    return `${y}-${m.padStart(2, '0')}-${d.padStart(2, '0')}`;
  }

  // dd-MM-yyyy
  if (/^\d{1,2}-\d{1,2}-\d{4}$/.test(clean)) {
    const [d, m, y] = clean.split('-');
    return `${y}-${m.padStart(2, '0')}-${d.padStart(2, '0')}`;
  }

  // yyyyMMdd (8 chữ số liền)
  if (/^\d{8}$/.test(clean)) {
    const y = clean.slice(0, 4);
    const m = clean.slice(4, 6);
    const d = clean.slice(6, 8);
    return `${y}-${m}-${d}`;
  }

  // yyyy-MM-dd ISO
  if (/^\d{4}-\d{2}-\d{2}/.test(clean)) {
    return clean.slice(0, 10);
  }

  return '';
}

/**
 * Chuyển chuỗi Base64 (hoặc data URI) thành đối tượng File để tải lên API Avatar
 */
export function base64ToFile(base64: string, filename = 'avatar_hn212.jpg'): File {
  let clean = base64.trim();
  let mimeType = 'image/jpeg';

  if (clean.startsWith('data:')) {
    const match = clean.match(/^data:(image\/[a-zA-Z+]+);base64,/);
    if (match) {
      mimeType = match[1];
      clean = clean.substring(match[0].length);
    }
  }

  const binaryStr = atob(clean);
  const len = binaryStr.length;
  const bytes = new Uint8Array(len);
  for (let i = 0; i < len; i++) {
    bytes[i] = binaryStr.charCodeAt(i);
  }

  return new File([bytes], filename, { type: mimeType });
}

type EventCallback<T = any> = (data: T) => void;

class Hn212Service {
  private ws: WebSocket | null = null;
  private reconnectTimer: any = null;
  private isConnecting = false;
  private listeners: Map<string, Set<EventCallback>> = new Map();
  public isConnected = false;

  constructor() {
    // Không tự động kết nối ngay trong constructor để an toàn với môi trường SSR/Test
  }

  /**
   * Đăng ký lắng nghe sự kiện từ HN212
   */
  public on<T = any>(event: string, callback: EventCallback<T>): () => void {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event)!.add(callback);

    return () => {
      this.off(event, callback);
    };
  }

  /**
   * Hủy đăng ký lắng nghe sự kiện
   */
  public off(event: string, callback: EventCallback): void {
    this.listeners.get(event)?.delete(callback);
  }

  private emit(event: string, data?: any): void {
    const cbs = this.listeners.get(event);
    if (cbs) {
      cbs.forEach((cb) => {
        try {
          cb(data);
        } catch (err) {
          console.error(`[HN212] Lỗi callback sự kiện ${event}:`, err);
        }
      });
    }
  }

  /**
   * Khởi động kết nối Native WebSocket tới SignalR Hub của HN212
   */
  public connect(): void {
    if (typeof window === 'undefined') return;
    if (import.meta.env?.MODE === 'test') return;
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) {
      return;
    }
    if (this.isConnecting) return;

    this.isConnecting = true;

    try {
      this.ws = new WebSocket(DEFAULT_HN212_WS);

      this.ws.onopen = () => {
        this.isConnecting = false;
        // Bắt tay giao thức SignalR JSON protocol
        const handshake = JSON.stringify({ protocol: 'json', version: 1 }) + RECORD_SEPARATOR;
        this.ws?.send(handshake);
      };

      this.ws.onmessage = (event) => {
        this.handleWsMessage(event.data);
      };

      this.ws.onclose = () => {
        this.isConnecting = false;
        if (this.isConnected) {
          this.isConnected = false;
          this.emit('connectionChanged', false);
        }
        this.scheduleReconnect();
      };

      this.ws.onerror = () => {
        this.isConnecting = false;
        // On error, onclose will fire
      };
    } catch {
      this.isConnecting = false;
      this.scheduleReconnect();
    }
  }

  private scheduleReconnect(): void {
    if (this.reconnectTimer) return;
    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = null;
      this.connect();
    }, 4000);
  }

  private handleWsMessage(raw: string): void {
    const packets = raw.split(RECORD_SEPARATOR);
    for (const packet of packets) {
      if (!packet.trim()) continue;

      try {
        const msg = JSON.parse(packet);

        // Bắt tay thành công (server phản hồi {})
        if (Object.keys(msg).length === 0) {
          this.isConnected = true;
          this.emit('connectionChanged', true);
          continue;
        }

        // Ping (Type 6) -> phản hồi pong
        if (msg.type === 6) {
          this.ws?.send(JSON.stringify({ type: 6 }) + RECORD_SEPARATOR);
          continue;
        }

        // Invocation từ Server (Type 1)
        if (msg.type === 1 && msg.target) {
          const target = msg.target as string;
          const args = msg.arguments || [];

          if (target === 'CardReadCompleted' && args[0]) {
            this.emit('CardReadCompleted', args[0]);
          } else if (target === 'FaceCaptured' && args[0]) {
            this.emit('FaceCaptured', args[0]);
          } else if (target === 'FaceCaptureStarted') {
            this.emit('FaceCaptureStarted');
          } else if (target === 'FaceCaptureCancelled') {
            this.emit('FaceCaptureCancelled', args[0]);
          } else if (target === 'DeviceStatusChanged' && args[0]) {
            this.emit('DeviceStatusChanged', args[0]);
          } else if (target === 'CardStatusChanged') {
            this.emit('CardStatusChanged', { status: args[0], message: args[1] });
          } else if (target === 'FaceCompared' && args[0]) {
            this.emit('FaceCompared', args[0]);
          }
        }
      } catch {
        // Bỏ qua gói tin không đúng định dạng
      }
    }
  }

  /**
   * Lấy trạng thái hiện tại của đầu đọc qua REST API
   */
  public async getStatus(): Promise<Hn212ReaderStatus | null> {
    if (import.meta.env?.MODE === 'test') return null;
    try {
      const res = await fetch(`${DEFAULT_HN212_HOST}/api/status`);
      if (!res.ok) return null;
      const json = await res.json();
      return json.data || null;
    } catch {
      return null;
    }
  }

  /**
   * Bật camera và bắt đầu chụp khuôn mặt tự động on-demand
   */
  public async startCaptureFace(): Promise<boolean> {
    try {
      const res = await fetch(`${DEFAULT_HN212_HOST}/api/face/start-capture`, {
        method: 'POST',
      });
      if (!res.ok) return false;
      const json = await res.json();
      return json.success ?? false;
    } catch {
      return false;
    }
  }

  /**
   * Hủy chụp và tắt camera ngay lập tức
   */
  public async cancelCaptureFace(): Promise<boolean> {
    try {
      const res = await fetch(`${DEFAULT_HN212_HOST}/api/face/cancel-capture`, {
        method: 'POST',
      });
      if (!res.ok) return false;
      const json = await res.json();
      return json.success ?? false;
    } catch {
      return false;
    }
  }

  /**
   * Kích hoạt đọc thẻ CCCD thủ công
   */
  public async readCardManual(): Promise<boolean> {
    try {
      const res = await fetch(`${DEFAULT_HN212_HOST}/api/card/read`, {
        method: 'POST',
      });
      if (!res.ok) return false;
      const json = await res.json();
      return json.success ?? false;
    } catch {
      return false;
    }
  }

  /**
   * Yêu cầu đầu đọc so khớp ảnh chân dung trong chip CCCD với ảnh camera vừa chụp
   */
  public async compareFace(): Promise<Hn212FaceCompareResult | null> {
    try {
      const res = await fetch(`${DEFAULT_HN212_HOST}/api/face/compare`, {
        method: 'POST',
      });
      if (!res.ok) return null;
      const json = await res.json();
      return json.data || null;
    } catch {
      return null;
    }
  }

  /**
   * URL luồng video MJPEG từ camera của đầu đọc HN212
   */
  public getCameraStreamUrl(): string {
    return `${DEFAULT_HN212_HOST}/api/camera/stream`;
  }
}

export const hn212Service = new Hn212Service();
