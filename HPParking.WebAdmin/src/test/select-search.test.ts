import { describe, it, expect } from 'vitest';
import {
  shouldShowSelectSearch,
  getSelectSearchField,
  filterSelectItems,
  normalizeVietnamese,
} from '@/lib/select-search';

describe('select-search utility', () => {
  describe('shouldShowSelectSearch', () => {
    it('trả về false khi số lượng phần tử <= 10', () => {
      expect(shouldShowSelectSearch(0)).toBe(false);
      expect(shouldShowSelectSearch(5)).toBe(false);
      expect(shouldShowSelectSearch(10)).toBe(false);
    });

    it('trả về true khi số lượng phần tử > 10', () => {
      expect(shouldShowSelectSearch(11)).toBe(true);
      expect(shouldShowSelectSearch(50)).toBe(true);
      expect(shouldShowSelectSearch(100)).toBe(true);
    });

    it('hỗ trợ tùy biến ngưỡng threshold khác nếu cần', () => {
      expect(shouldShowSelectSearch(5, 4)).toBe(true);
      expect(shouldShowSelectSearch(5, 5)).toBe(false);
    });
  });

  describe('getSelectSearchField - Ưu tiên 1: Tìm theo MÃ', () => {
    it('nhận diện trường "code" chuẩn', () => {
      const sample = { id: '1', code: 'CLI-001', name: 'Nguyễn Văn A' };
      const result = getSelectSearchField(sample);
      expect(result.field).toBe('code');
      expect(result.isCode).toBe(true);
      expect(result.placeholder).toBe('Tìm theo mã...');
    });

    it('nhận diện trường "*Code" (routeCode, gateCode, deviceCode...)', () => {
      const route = { id: 'r1', routeCode: 'ROUTE_01', routeName: 'Tuyến số 1' };
      const gate = { id: 'g1', gateCode: 'GATE_01', name: 'Cổng chính' };
      const device = { id: 'd1', deviceCode: 'DEV_01', ipAddress: '192.168.1.1' };

      expect(getSelectSearchField(route).field).toBe('routeCode');
      expect(getSelectSearchField(gate).field).toBe('gateCode');
      expect(getSelectSearchField(device).field).toBe('deviceCode');
    });

    it('nhận diện trường biển số "plateNumber"', () => {
      const vehicle = { id: 'v1', plateNumber: '30A-123.45', type: 'Xe tải' };
      const result = getSelectSearchField(vehicle);
      expect(result.field).toBe('plateNumber');
      expect(result.isCode).toBe(true);
      expect(result.placeholder).toBe('Tìm theo biển số...');
    });

    it('nhận diện trường mã thẻ "cardNumber"', () => {
      const card = { id: 'c1', cardNumber: '0012345678', status: 1 };
      const result = getSelectSearchField(card);
      expect(result.field).toBe('cardNumber');
      expect(result.isCode).toBe(true);
      expect(result.placeholder).toBe('Tìm theo mã thẻ...');
    });

    it('nhận diện trường tên đăng nhập "username"', () => {
      const user = { id: 'u1', username: 'admin', fullName: 'Quản trị viên' };
      const result = getSelectSearchField(user);
      expect(result.field).toBe('username');
      expect(result.isCode).toBe(true);
      expect(result.placeholder).toBe('Tìm theo tên đăng nhập...');
    });
  });

  describe('getSelectSearchField - Ưu tiên 2: Nếu không có mã, tìm theo trường có độ nhận diện lớn nhất ngoài id', () => {
    it('nhận diện trường "name" khi không có mã', () => {
      const item = { id: 'comp-1', name: 'Công ty Cổ phần Thép Hòa Phát', address: 'Hải Dương' };
      const result = getSelectSearchField(item);
      expect(result.field).toBe('name');
      expect(result.isCode).toBe(false);
      expect(result.placeholder).toBe('Tìm theo tên...');
    });

    it('nhận diện trường "fullName" khi không có mã và không có name', () => {
      const item = { id: 'u2', fullName: 'Trần Thị B', email: 'b@example.com' };
      const result = getSelectSearchField(item);
      expect(result.field).toBe('fullName');
      expect(result.isCode).toBe(false);
      expect(result.placeholder).toBe('Tìm theo tên...');
    });

    it('nhận diện trường "title" hoặc "label"', () => {
      const option = { id: 'opt-1', title: 'Thông báo bảo trì', status: 'active' };
      const result = getSelectSearchField(option);
      expect(result.field).toBe('title');
      expect(result.placeholder).toBe('Tìm theo tiêu đề...');
    });

    it('nhận diện trường "phoneNumber" khi không có tên hay mã', () => {
      const contact = { id: 'c-1', phoneNumber: '0912345678', note: 'Ghi chú khẩn' };
      const result = getSelectSearchField(contact);
      expect(result.field).toBe('phoneNumber');
      expect(result.placeholder).toBe('Tìm theo số điện thoại...');
    });

    it('nhận diện trường "email" khi chỉ có id và email', () => {
      const account = { id: 'a-1', email: 'test@hpparking.vn' };
      const result = getSelectSearchField(account);
      expect(result.field).toBe('email');
      expect(result.placeholder).toBe('Tìm theo email...');
    });

    it('bỏ qua các trường ID hệ thống và boolean/meta', () => {
      const complexItem = {
        id: '6abb3cb91f3a0cf5469cf5cb',
        companyId: 'comp-1',
        departmentId: 'dept-1',
        isDeleted: false,
        isActive: true,
        createdAt: '2026-01-01',
        name: 'Phòng Kỹ Thuật',
      };
      const result = getSelectSearchField(complexItem);
      expect(result.field).toBe('name');
    });
  });

  describe('filterSelectItems', () => {
    const clients = [
      { id: '1', code: 'KH-001', name: 'Nguyễn Văn An' },
      { id: '2', code: 'KH-002', name: 'Trần Thị Bình' },
      { id: '3', code: 'VIP-999', name: 'Lê Văn Cường' },
    ];

    it('lọc chính xác theo mã', () => {
      const results = filterSelectItems(clients, 'VIP', 'code');
      expect(results).toHaveLength(1);
      expect(results[0].code).toBe('VIP-999');
    });

    it('hỗ trợ tìm kiếm tiếng Việt không dấu', () => {
      const results = filterSelectItems(clients, 'nguyen van an', 'code');
      expect(results).toHaveLength(1);
      expect(results[0].name).toBe('Nguyễn Văn An');
    });

    it('trả về toàn bộ danh sách khi từ khóa rỗng', () => {
      expect(filterSelectItems(clients, '')).toEqual(clients);
      expect(filterSelectItems(clients, '   ')).toEqual(clients);
    });
  });

  describe('normalizeVietnamese', () => {
    it('bỏ dấu tiếng Việt và chuyển thường', () => {
      expect(normalizeVietnamese('Đà Nẵng')).toBe('da nang');
      expect(normalizeVietnamese('HÒA PHÁT')).toBe('hoa phat');
      expect(normalizeVietnamese('30A-999.99')).toBe('30a-999.99');
    });
  });
});
