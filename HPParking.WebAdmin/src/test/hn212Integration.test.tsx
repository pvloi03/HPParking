import { useState } from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { parseCccdDate, base64ToFile, hn212Service } from '@/services/hn212Service';
import { ClientFormDialog } from '@/components/clients/ClientFormDialog';
import { Hn212CameraDialog } from '@/components/clients/Hn212CameraDialog';
import type { Hn212CardData } from '@/services/hn212Service';

describe('HN212 Smart Reader Integration', () => {
  describe('Helper parseCccdDate', () => {
    it('chuyển đổi đúng định dạng ngày tháng từ dd/MM/yyyy sang yyyy-MM-dd', () => {
      expect(parseCccdDate('25/09/1990')).toBe('1990-09-25');
      expect(parseCccdDate('05/01/2000')).toBe('2000-01-05');
      expect(parseCccdDate('1/2/1985')).toBe('1985-02-01');
    });

    it('chuyển đổi đúng định dạng dd-MM-yyyy và yyyyMMdd', () => {
      expect(parseCccdDate('25-09-1990')).toBe('1990-09-25');
      expect(parseCccdDate('19900925')).toBe('1990-09-25');
      expect(parseCccdDate('1995-12-31')).toBe('1995-12-31');
    });

    it('trả về chuỗi rỗng khi chuỗi ngày không hợp lệ hoặc null/undefined', () => {
      expect(parseCccdDate('')).toBe('');
      expect(parseCccdDate(undefined)).toBe('');
      expect(parseCccdDate('invalid-date')).toBe('');
    });
  });

  describe('Helper base64ToFile', () => {
    it('chuyển đổi chuỗi base64 thành đối tượng File hợp lệ', () => {
      // 1x1 transparent PNG base64
      const base64 = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
      const file = base64ToFile(base64, 'test_avatar.jpg');

      expect(file).toBeInstanceOf(File);
      expect(file.name).toBe('test_avatar.jpg');
      expect(file.size).toBeGreaterThan(0);
    });

    it('xử lý an toàn chuỗi base64 có tiền tố data:image/...;base64,', () => {
      const dataUri = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
      const file = base64ToFile(dataUri, 'data_uri.png');

      expect(file).toBeInstanceOf(File);
      expect(file.type).toBe('image/png');
      expect(file.size).toBeGreaterThan(0);
    });
  });

  describe('ClientFormDialog với dữ liệu CCCD từ HN212', () => {
    const dummyHn212Card: Hn212CardData = {
      DocumentNumber: '001200012345',
      FullName: 'NGUYỄN VĂN AN',
      DateOfBirth: '15/08/1992',
      Sex: 'Nam',
      PermanentAddress: 'Số 123 Lê Hồng Phong, Hải Phòng',
      ChipFaceBase64: 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
    };

    it('tự động điền thông tin từ CCCD và cho phép người dùng chỉnh sửa các trường', () => {
      render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          hn212CardData={dummyHn212Card}
          companies={[]}
          departments={[]}
          contractors={[]}
          onSubmit={vi.fn()}
        />
      );

      // Kiểm tra tiêu đề hiển thị đúng chế độ tự động từ CCCD
      expect(screen.getByText(/Đăng Ký Khách Hàng \(Từ Thẻ CCCD\)/i)).toBeInTheDocument();

      // Kiểm tra banner thông báo
      expect(
        screen.getByText(/Dữ liệu được điền tự động từ thẻ chip CCCD/i)
      ).toBeInTheDocument();

      // Kiểm tra các trường được điền tự động nhưng KHÔNG bị khóa (cho phép sửa)
      const codeInput = screen.getByPlaceholderText('VD: 001200012345') as HTMLInputElement;
      expect(codeInput.value).toBe('001200012345');
      expect(codeInput).not.toHaveAttribute('readonly');

      const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam') as HTMLInputElement;
      expect(nameInput.value).toBe('NGUYỄN VĂN AN');
      expect(nameInput).not.toHaveAttribute('readonly');

      const addressInput = screen.getByPlaceholderText('VD: Hải Phòng, Việt Nam') as HTMLInputElement;
      expect(addressInput.value).toBe('Số 123 Lê Hồng Phong, Hải Phòng');
      expect(addressInput).not.toHaveAttribute('readonly');

      // Kiểm tra nút máy ảnh chụp từ HN212 vẫn sẵn sàng và KHÔNG bị disable
      const cameraBtn = screen.getByTitle('Chụp ảnh trực tiếp từ đầu đọc HN212');
      expect(cameraBtn).toBeInTheDocument();
      expect(cameraBtn).not.toBeDisabled();
    });

    it('hiển thị tiêu đề Cập Nhật Hồ Sơ khi có cả initialData và hn212CardData', () => {
      const existingClient: any = {
        id: 'client-1',
        code: '001200012345',
        name: 'NGUYỄN CŨ',
        phoneNumber: '0912345678',
        gender: 1,
        birthDay: '1990-01-01',
        address: 'Hà Nội',
        isActive: true,
      };

      render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          initialData={existingClient}
          hn212CardData={dummyHn212Card}
          companies={[]}
          departments={[]}
          contractors={[]}
          onSubmit={vi.fn()}
        />
      );

      // Tiêu đề là Cập nhật hồ sơ khách hàng
      expect(screen.getByText(/Cập Nhật Hồ Sơ Khách Hàng \(Từ Thẻ CCCD\)/i)).toBeInTheDocument();

      // Họ tên được cập nhật từ thẻ CCCD mới đọc
      const nameInput = screen.getByPlaceholderText('VD: Nguyễn Văn Nam') as HTMLInputElement;
      expect(nameInput.value).toBe('NGUYỄN VĂN AN');
      expect(nameInput).not.toHaveAttribute('readonly');
    });

    it('giữ nguyên avatar hiện tại khi client đã có avatar và hiển thị nút Dùng ảnh CCCD', () => {
      const existingClientWithAvatar: any = {
        id: 'client-1',
        code: '001200012345',
        name: 'NGUYỄN CŨ',
        phoneNumber: '0912345678',
        gender: 1,
        birthDay: '1990-01-01',
        address: 'Hà Nội',
        avatar: 'https://example.com/client-avatar.jpg',
        isActive: true,
      };

      render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          initialData={existingClientWithAvatar}
          hn212CardData={dummyHn212Card}
          companies={[]}
          departments={[]}
          contractors={[]}
          onSubmit={vi.fn()}
        />
      );

      // Avatar hiện tại vẫn là ảnh cũ của client
      const avatarImg = screen.getByAltText('Avatar khách hàng');
      expect(avatarImg).toHaveAttribute('src', 'https://example.com/client-avatar.jpg');

      // Có nút để chọn ảnh từ CCCD
      expect(screen.getByRole('button', { name: /Dùng ảnh CCCD/i })).toBeInTheDocument();
    });

    it('hiển thị nút Thêm phương tiện và mở các trường nhập liệu khi người dùng nhấn', async () => {
      render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          companies={[]}
          departments={[]}
          contractors={[]}
          onSubmit={vi.fn()}
        />
      );

      // Ban đầu chưa có trường biển số xe
      expect(screen.queryByPlaceholderText(/30A-123.45/i)).not.toBeInTheDocument();

      // Có nút Thêm phương tiện
      const addVehicleButtons = screen.getAllByRole('button', { name: /Thêm phương tiện/i });
      expect(addVehicleButtons.length).toBeGreaterThan(0);

      // Nhấn nút Thêm phương tiện
      await act(async () => {
        addVehicleButtons[0].click();
      });

      // Xuất hiện trường nhập Biển số xe và Ghi chú phương tiện
      expect(screen.getByPlaceholderText(/30A-123.45/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/Xe cá nhân, xe cơ quan/i)).toBeInTheDocument();
      expect(screen.getByText(/Phương tiện mới #1/i)).toBeInTheDocument();

      // Có nút Xóa phương tiện
      const deleteVehicleBtn = screen.getByTitle('Xóa phương tiện này');
      expect(deleteVehicleBtn).toBeInTheDocument();

      // Nhấn Xóa phương tiện
      await act(async () => {
        deleteVehicleBtn.click();
      });

      // Trường nhập bị xóa khỏi form
      expect(screen.queryByPlaceholderText(/30A-123.45/i)).not.toBeInTheDocument();
    });

    it('chỉ hiển thị Công ty và Phòng ban khi chọn Cán bộ nhân viên, và Nhà thầu khi chọn Nhân sự nhà thầu', () => {
      const employeeClient: any = {
        id: 'client-emp',
        code: '001200000001',
        name: 'NHÂN VIÊN A',
        type: 0, // ClientType.Employee
        phoneNumber: '0912345678',
        gender: 1,
        birthDay: '1995-01-01',
        address: 'Hà Nội',
        isActive: true,
      };

      const contractorClient: any = {
        id: 'client-con',
        code: '001200000002',
        name: 'NHÀ THẦU B',
        type: 1, // ClientType.Contractor
        phoneNumber: '0987654321',
        gender: 1,
        birthDay: '1992-05-10',
        address: 'Hải Phòng',
        isActive: true,
      };

      const visitorClient: any = {
        id: 'client-vis',
        code: '001200000003',
        name: 'KHÁCH VÃNG LAI C',
        type: 2, // ClientType.Visitor
        phoneNumber: '0901234567',
        gender: 0,
        birthDay: '1998-09-20',
        address: 'Đà Nẵng',
        isActive: true,
      };

      // 1. Trường hợp Cán bộ nhân viên: Hiển thị Công ty & Phòng ban, KHÔNG hiển thị Nhà thầu
      const { unmount: unmount1 } = render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          initialData={employeeClient}
          companies={[{ id: 'c1', code: 'HP', name: 'Hoàng Phát', isActive: true, createdAt: '' }]}
          departments={[{ id: 'd1', code: 'IT', name: 'Công nghệ', companyId: 'c1', isActive: true, createdAt: '' }]}
          contractors={[{ id: 'con1', code: 'NT1', name: 'Xây dựng Delta', isActive: true, createdAt: '' }]}
          onSubmit={vi.fn()}
        />
      );
      expect(screen.getByText(/Công ty trực thuộc/i)).toBeInTheDocument();
      expect(screen.getByText(/^Phòng ban$/i)).toBeInTheDocument();
      expect(screen.queryByText(/Nhà thầu đối tác/i)).not.toBeInTheDocument();
      unmount1();

      // 2. Trường hợp Nhà thầu: Hiển thị Nhà thầu, KHÔNG hiển thị Công ty & Phòng ban
      const { unmount: unmount2 } = render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          initialData={contractorClient}
          companies={[{ id: 'c1', code: 'HP', name: 'Hoàng Phát', isActive: true, createdAt: '' }]}
          departments={[{ id: 'd1', code: 'IT', name: 'Công nghệ', companyId: 'c1', isActive: true, createdAt: '' }]}
          contractors={[{ id: 'con1', code: 'NT1', name: 'Xây dựng Delta', isActive: true, createdAt: '' }]}
          onSubmit={vi.fn()}
        />
      );
      expect(screen.getByText(/Nhà thầu đối tác/i)).toBeInTheDocument();
      expect(screen.queryByText(/Công ty trực thuộc/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/^Phòng ban$/i)).not.toBeInTheDocument();
      unmount2();

      // 3. Trường hợp Khách vãng lai: KHÔNG hiển thị cả Công ty, Phòng ban và Nhà thầu
      render(
        <ClientFormDialog
          open={true}
          onOpenChange={vi.fn()}
          initialData={visitorClient}
          companies={[{ id: 'c1', code: 'HP', name: 'Hoàng Phát', isActive: true, createdAt: '' }]}
          departments={[{ id: 'd1', code: 'IT', name: 'Công nghệ', companyId: 'c1', isActive: true, createdAt: '' }]}
          contractors={[{ id: 'con1', code: 'NT1', name: 'Xây dựng Delta', isActive: true, createdAt: '' }]}
          onSubmit={vi.fn()}
        />
      );
      expect(screen.queryByText(/Công ty trực thuộc/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/^Phòng ban$/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/Nhà thầu đối tác/i)).not.toBeInTheDocument();
      expect(screen.getByText(/không áp dụng gắn Công ty, Phòng ban hoặc Nhà thầu/i)).toBeInTheDocument();
    });
  });

  describe('Hn212CameraDialog lifecycle & loop prevention', () => {
    it('đóng dialog và không kích hoạt stream mới khi chụp ảnh thành công và component cha re-render', async () => {
      vi.useFakeTimers();

      vi.spyOn(hn212Service, 'getStatus').mockResolvedValue({
        isReaderConnected: true,
        readerSerialNumber: 'HANEL-123',
        cardStatus: 'None',
        isCameraActive: false,
      });
      const startCaptureSpy = vi.spyOn(hn212Service, 'startCaptureFace').mockResolvedValue(true);

      const onOpenChange = vi.fn();

      // Parent component mô phỏng AvatarUploadField re-render khi setPreviewUrl
      function ParentComponent() {
        const [, setCounter] = useState(0);
        return (
          <Hn212CameraDialog
            open={true}
            onOpenChange={onOpenChange}
            onCaptureSuccess={() => {
              setCounter((c) => c + 1);
            }}
          />
        );
      }

      render(<ParentComponent />);

      await vi.waitFor(() => {
        expect(startCaptureSpy).toHaveBeenCalledTimes(1);
      });

      // Kích hoạt sự kiện FaceCaptured từ thiết bị trong act để flush render
      act(() => {
        (hn212Service as any).emit('FaceCaptured', 'dummyBase64');
      });

      // Tua qua 850ms để timeout đóng dialog
      act(() => {
        vi.advanceTimersByTime(850);
      });

      // Kiểm tra startCaptureFace KHÔNG bị gọi lại lần thứ 2
      expect(startCaptureSpy).toHaveBeenCalledTimes(1);

      // onOpenChange(false) PHẢI được gọi để đóng dialog
      expect(onOpenChange).toHaveBeenCalledWith(false);

      vi.useRealTimers();
    });
  });
});
