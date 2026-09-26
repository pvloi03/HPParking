import { describe, it, expect, beforeEach } from 'vitest';
import {
  useDevicePingStore,
  isPingRecordFresh,
  PING_TTL_MS,
} from '@/stores/devicePingStore';
import type { DevicePingResultDto } from '@/types/infrastructure';

describe('devicePingStore (Zustand with LocalStorage persistence)', () => {
  beforeEach(() => {
    // Reset store and clear localStorage before each test
    localStorage.clear();
    useDevicePingStore.getState().clearAllRecords();
    useDevicePingStore.getState().setAutoPingEnabled(true);
    useDevicePingStore.getState().setBatchPinging(false);
  });

  const mockPingResult1: DevicePingResultDto = {
    ipAddress: '192.168.1.50',
    isAlive: true,
    roundtripTimeMs: 15,
    method: 'ICMP',
    message: 'Online 15ms',
    timestamp: new Date().toISOString(),
  };

  const mockPingResult2: DevicePingResultDto = {
    ipAddress: '192.168.1.60',
    isAlive: false,
    roundtripTimeMs: 2000,
    method: 'NONE',
    message: 'Timeout',
    timestamp: new Date().toISOString(),
  };

  it('khởi tạo với trạng thái mặc định chính xác', () => {
    const state = useDevicePingStore.getState();
    expect(state.records).toEqual({});
    expect(state.isAutoPingEnabled).toBe(true);
    expect(state.isBatchPinging).toBe(false);
    expect(state.pingingDeviceIds).toEqual({});
  });

  it('cập nhật kết quả ping đơn lẻ qua setPingResult và gán lastPingedAt', () => {
    useDevicePingStore.getState().setPingResult('dev-1', mockPingResult1);

    const record = useDevicePingStore.getState().records['dev-1'];
    expect(record).toBeDefined();
    expect(record.result).toEqual(mockPingResult1);
    expect(record.lastPingedAt).toBeGreaterThan(0);
    expect(Date.now() - record.lastPingedAt).toBeLessThan(1000);
  });

  it('cập nhật kết quả hàng loạt qua setBatchPingResults', () => {
    useDevicePingStore.getState().setBatchPingResults([
      { deviceId: 'dev-1', result: mockPingResult1 },
      { deviceId: 'dev-2', result: mockPingResult2 },
    ]);

    const records = useDevicePingStore.getState().records;
    expect(records['dev-1'].result.isAlive).toBe(true);
    expect(records['dev-2'].result.isAlive).toBe(false);
    expect(records['dev-1'].result.roundtripTimeMs).toBe(15);
  });

  it('hàm isPingRecordFresh kiểm tra độ tươi dữ liệu theo TTL 5 phút chuẩn xác', () => {
    const now = Date.now();

    // Dữ liệu vừa ping (cách đây 1 giây) -> Còn tươi
    const freshRecord = {
      result: mockPingResult1,
      lastPingedAt: now - 1000,
    };
    expect(isPingRecordFresh(freshRecord)).toBe(true);

    // Dữ liệu cách đây 4 phút -> Vẫn còn tươi
    const fourMinutesOldRecord = {
      result: mockPingResult1,
      lastPingedAt: now - 4 * 60 * 1000,
    };
    expect(isPingRecordFresh(fourMinutesOldRecord)).toBe(true);

    // Dữ liệu cách đây 6 phút (> TTL 5 phút) -> Hết tươi (stale)
    const staleRecord = {
      result: mockPingResult1,
      lastPingedAt: now - 6 * 60 * 1000,
    };
    expect(isPingRecordFresh(staleRecord)).toBe(false);

    // Không có record hoặc lastPingedAt không hợp lệ -> false
    expect(isPingRecordFresh(undefined)).toBe(false);
  });

  it('định nghĩa hằng số TTL mặc định là 5 phút (300,000ms)', () => {
    expect(PING_TTL_MS).toBe(5 * 60 * 1000);
  });

  it('bật/tắt công tắc tự động ping qua setAutoPingEnabled', () => {
    expect(useDevicePingStore.getState().isAutoPingEnabled).toBe(true);

    useDevicePingStore.getState().setAutoPingEnabled(false);
    expect(useDevicePingStore.getState().isAutoPingEnabled).toBe(false);

    useDevicePingStore.getState().setAutoPingEnabled(true);
    expect(useDevicePingStore.getState().isAutoPingEnabled).toBe(true);
  });

  it('quản lý cờ loading setPinging và setBatchPinging', () => {
    useDevicePingStore.getState().setPinging('dev-1', true);
    expect(useDevicePingStore.getState().pingingDeviceIds['dev-1']).toBe(true);

    useDevicePingStore.getState().setPinging('dev-1', false);
    expect(useDevicePingStore.getState().pingingDeviceIds['dev-1']).toBe(false);

    useDevicePingStore.getState().setBatchPinging(true);
    expect(useDevicePingStore.getState().isBatchPinging).toBe(true);

    useDevicePingStore.getState().setBatchPinging(false);
    expect(useDevicePingStore.getState().isBatchPinging).toBe(false);
  });

  it('xóa bản ghi đơn lẻ removeRecord và xóa toàn bộ clearAllRecords', () => {
    useDevicePingStore.getState().setBatchPingResults([
      { deviceId: 'dev-1', result: mockPingResult1 },
      { deviceId: 'dev-2', result: mockPingResult2 },
    ]);

    useDevicePingStore.getState().removeRecord('dev-1');
    expect(useDevicePingStore.getState().records['dev-1']).toBeUndefined();
    expect(useDevicePingStore.getState().records['dev-2']).toBeDefined();

    useDevicePingStore.getState().clearAllRecords();
    expect(useDevicePingStore.getState().records).toEqual({});
  });

  it('lưu trữ dữ liệu vào localStorage với khóa hp-parking-device-ping-storage', () => {
    useDevicePingStore.getState().setPingResult('dev-1', mockPingResult1);
    useDevicePingStore.getState().setAutoPingEnabled(false);
    useDevicePingStore.getState().setBatchPinging(true); // temporary flag, should NOT persist

    const raw = localStorage.getItem('hp-parking-device-ping-storage');
    expect(raw).not.toBeNull();

    const parsed = JSON.parse(raw!);
    expect(parsed.state.records['dev-1']).toBeDefined();
    expect(parsed.state.isAutoPingEnabled).toBe(false);
    // Cờ tạm thời không được persist vào localStorage
    expect(parsed.state.isBatchPinging).toBeUndefined();
    expect(parsed.state.pingingDeviceIds).toBeUndefined();
  });
});
