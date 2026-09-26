import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { DevicePingResultDto } from '@/types/infrastructure';

export const PING_TTL_MS = 5 * 60 * 1000; // 5 phút

export interface PingRecord {
  result: DevicePingResultDto;
  lastPingedAt: number; // Unix timestamp ms (Date.now())
}

export interface DevicePingState {
  // Bản đồ lưu kết quả ping theo Device ID: { [deviceId: string]: PingRecord }
  records: Record<string, PingRecord>;

  // Bản đồ lưu trạng thái đang gửi gói tin ping: { [deviceId: string]: boolean }
  pingingDeviceIds: Record<string, boolean>;

  // Cờ trạng thái bật/tắt tự động ping chu kỳ 15s tại trang danh sách (Mặc định: true)
  isAutoPingEnabled: boolean;

  // Cờ trạng thái batch ping toàn trang đang chạy
  isBatchPinging: boolean;
}

export interface DevicePingActions {
  // Cập nhật kết quả ping cho một thiết bị cụ thể
  setPingResult: (deviceId: string, result: DevicePingResultDto) => void;

  // Cập nhật hàng loạt kết quả ping từ API batch ping
  setBatchPingResults: (
    results: { deviceId: string; result: DevicePingResultDto }[]
  ) => void;

  // Thiết lập trạng thái loading cho một thiết bị
  setPinging: (deviceId: string, isPinging: boolean) => void;

  // Thiết lập trạng thái loading cho batch ping
  setBatchPinging: (isBatch: boolean) => void;

  // Bật / tắt tính năng tự động ping 15s
  setAutoPingEnabled: (enabled: boolean) => void;

  // Xóa bản ghi đã lưu của một thiết bị
  removeRecord: (deviceId: string) => void;

  // Xóa toàn bộ bản ghi
  clearAllRecords: () => void;
}

export type DevicePingStore = DevicePingState & DevicePingActions;

/**
 * Kiểm tra xem bản ghi ping có còn trong thời gian tươi (Fresh) hay không.
 * Nếu quá TTL (mặc định 5 phút) thì coi là hết hạn (stale) cần đo kiểm lại.
 */
export function isPingRecordFresh(
  record?: PingRecord,
  ttlMs = PING_TTL_MS
): boolean {
  if (!record || !record.lastPingedAt) return false;
  return Date.now() - record.lastPingedAt < ttlMs;
}

export const useDevicePingStore = create<DevicePingStore>()(
  persist(
    (set) => ({
      records: {},
      pingingDeviceIds: {},
      isAutoPingEnabled: true,
      isBatchPinging: false,

      setPingResult: (deviceId, result) =>
        set((state) => ({
          records: {
            ...state.records,
            [deviceId]: {
              result,
              lastPingedAt: Date.now(),
            },
          },
        })),

      setBatchPingResults: (results) =>
        set((state) => {
          const now = Date.now();
          const nextRecords = { ...state.records };
          for (const item of results) {
            nextRecords[item.deviceId] = {
              result: item.result,
              lastPingedAt: now,
            };
          }
          return { records: nextRecords };
        }),

      setPinging: (deviceId, isPinging) =>
        set((state) => ({
          pingingDeviceIds: {
            ...state.pingingDeviceIds,
            [deviceId]: isPinging,
          },
        })),

      setBatchPinging: (isBatchPinging) => set({ isBatchPinging }),

      setAutoPingEnabled: (isAutoPingEnabled) => set({ isAutoPingEnabled }),

      removeRecord: (deviceId) =>
        set((state) => {
          const nextRecords = { ...state.records };
          delete nextRecords[deviceId];
          return { records: nextRecords };
        }),

      clearAllRecords: () => set({ records: {} }),
    }),
    {
      name: 'hp-parking-device-ping-storage',
      // Chỉ lưu trữ records và isAutoPingEnabled vào localStorage
      // Loại trừ các cờ loading tạm thời để không bị treo khi mở lại trình duyệt
      partialize: (state) => ({
        records: state.records,
        isAutoPingEnabled: state.isAutoPingEnabled,
      }),
    }
  )
);
