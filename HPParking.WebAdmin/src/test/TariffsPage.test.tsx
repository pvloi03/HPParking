import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { TariffsPage } from '@/pages/TariffsPage';
import { tariffApi } from '@/api/tariffApi';
import { TariffPricingMode, type TariffDto } from '@/types/tariff';
import { VehicleType } from '@/types/vehicle';

vi.mock('@/api/tariffApi', () => ({
  tariffApi: {
    getPaged: vi.fn(),
    getAllActive: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    restore: vi.fn(),
  },
  extractErrorMessage: vi.fn((err: any) => err?.message || 'Error'),
}));

vi.mock('@/hooks/usePermissions', () => ({
  usePermissions: () => ({
    canWrite: true,
  }),
}));

describe('TariffsPage Component', () => {
  let queryClient: QueryClient;

  const mockTariffs: TariffDto[] = [
    {
      id: 'tariff-1',
      code: 'TARIFF_CAR_FLAT',
      name: 'Biểu phí ô tô lượt',
      vehicleType: VehicleType.Car,
      pricingMode: TariffPricingMode.Flat,
      gracePeriodMinutes: 15,
      flatPrice: 15000,
      basePrice: 0,
      baseBlockMinutes: 0,
      additionalBlockPrice: 0,
      additionalBlockMinutes: 0,
      overnightSurcharge: 25000,
      maxDailyCap: 100000,
      isActive: true,
      createdAt: '2026-10-27T00:00:00Z',
    },
    {
      id: 'tariff-2',
      code: 'TARIFF_MOTO_BLOCK',
      name: 'Biểu phí xe máy theo block',
      vehicleType: VehicleType.Motorbike,
      pricingMode: TariffPricingMode.Block,
      gracePeriodMinutes: 10,
      flatPrice: 0,
      basePrice: 5000,
      baseBlockMinutes: 120,
      additionalBlockPrice: 2000,
      additionalBlockMinutes: 60,
      overnightSurcharge: 10000,
      maxDailyCap: 30000,
      isActive: true,
      createdAt: '2026-10-27T00:00:00Z',
    },
  ];

  beforeEach(() => {
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
    vi.clearAllMocks();

    vi.mocked(tariffApi.getPaged).mockResolvedValue({
      items: mockTariffs,
      pagination: {
        pageIndex: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
    });
  });

  const renderWithProviders = (ui: React.ReactElement) => {
    return render(
      <QueryClientProvider client={queryClient}>
        {ui}
      </QueryClientProvider>
    );
  };

  it('hiển thị tiêu đề và danh sách biểu phí với các chế độ tính phí', async () => {
    renderWithProviders(<TariffsPage />);

    expect(screen.getByText('Quản Lý Biểu Phí Đỗ Xe')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getAllByText('TARIFF_CAR_FLAT').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Biểu phí ô tô lượt').length).toBeGreaterThan(0);
      expect(screen.getAllByText('TARIFF_MOTO_BLOCK').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Biểu phí xe máy theo block').length).toBeGreaterThan(0);
    });

    expect(tariffApi.getPaged).toHaveBeenCalledTimes(1);
  });

  it('mở modal thêm mới khi nhấn nút Thêm biểu phí', async () => {
    renderWithProviders(<TariffsPage />);

    await waitFor(() => {
      expect(screen.getAllByText('TARIFF_CAR_FLAT').length).toBeGreaterThan(0);
    });

    const addButtons = screen.getAllByRole('button', { name: /Thêm biểu phí/i });
    expect(addButtons.length).toBeGreaterThan(0);
    fireEvent.click(addButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('Thêm Biểu Phí Đỗ Xe Mới')).toBeInTheDocument();
    });
  });
});
