import { describe, it, expect } from 'vitest';
import { findDefaultHrDepartmentId } from '@/utils/departmentHelper';
import type { DepartmentDto } from '@/types/masterData';

describe('departmentHelper - findDefaultHrDepartmentId', () => {
  it('trả về chuỗi rỗng khi danh sách phòng ban trống', () => {
    expect(findDefaultHrDepartmentId([])).toBe('');
  });

  it('ưu tiên nhận diện phòng ban chứa "nhân sự" có dấu hoặc không dấu', () => {
    const depts: DepartmentDto[] = [
      { id: 'dept-1', name: 'Phòng Kỹ thuật', code: 'KT', companyId: 'c1', isActive: true, createdAt: '' },
      { id: 'dept-2', name: 'Phòng Hành chính - Nhân sự', code: 'NS', companyId: 'c1', isActive: true, createdAt: '' },
      { id: 'dept-3', name: 'Phòng Kế toán', code: 'TC', companyId: 'c1', isActive: true, createdAt: '' },
    ];

    expect(findDefaultHrDepartmentId(depts)).toBe('dept-2');
  });

  it('nhận diện phòng ban chứa từ khóa "HR" độc lập', () => {
    const depts: DepartmentDto[] = [
      { id: 'dept-it', name: 'Phòng Công nghệ', code: 'IT', companyId: 'c1', isActive: true, createdAt: '' },
      { id: 'dept-hr', name: 'HR Department', code: 'HR', companyId: 'c1', isActive: true, createdAt: '' },
    ];

    expect(findDefaultHrDepartmentId(depts)).toBe('dept-hr');
  });

  it('chọn phòng ban đầu tiên nếu không có phòng ban nào khớp từ khóa nhân sự/HR', () => {
    const depts: DepartmentDto[] = [
      { id: 'dept-first', name: 'Phòng Kinh doanh', code: 'KD', companyId: 'c1', isActive: true, createdAt: '' },
      { id: 'dept-second', name: 'Phòng Marketing', code: 'MKT', companyId: 'c1', isActive: true, createdAt: '' },
    ];

    expect(findDefaultHrDepartmentId(depts)).toBe('dept-first');
  });
});
