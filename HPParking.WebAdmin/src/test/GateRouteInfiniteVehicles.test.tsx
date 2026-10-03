import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GateRouteFormDialog } from '@/components/routes/GateRouteFormDialog';
import { vehicleApi } from '@/api/vehicleApi';
import { gatesApi } from '@/api/infrastructureApi';
import { VehicleType } from '@/types/vehicle';
import type { PagedResult } from '@/types/masterData';

vi.mock('@/api/vehicleApi', () => ({
  vehicleApi: {
    getPaged: vi.fn(),
  },
}));

vi.mock('@/api/infrastructureApi', () => ({
  gatesApi: {
    getPaged: vi.fn(),
  },
}));

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
  );
}

describe('GateRouteFormDialog Infinite Vehicles Assignment', () => {
  beforeEach(() => {
    vi.clearAllMocks();

    vi.mocked(gatesApi.getPaged).mockResolvedValue({
      items: [
        { id: 'gate-1', name: 'Cổng 1', code: 'G01' } as any,
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 200,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    });

    vi.mocked(vehicleApi.getPaged).mockResolvedValue({
      items: [
        {
          id: 'v-1',
          plateNumber: '29A-11111',
          type: VehicleType.Car,
          cardCode: 'CARD01',
          isShared: true,
          isActive: true,
        },
        {
          id: 'v-2',
          plateNumber: '29A-22222',
          type: VehicleType.Car,
          cardCode: 'CARD02',
          isShared: true,
          isActive: true,
        },
      ],
      pagination: {
        pageIndex: 1,
        pageSize: 20,
        totalCount: 2,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
      },
    } as PagedResult<any>);
  });

  it('loads shared vehicles using paged infinite query and displays them', async () => {
    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={vi.fn()}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('29A-11111')).toBeInTheDocument();
      expect(screen.getByText('29A-22222')).toBeInTheDocument();
    });

    expect(screen.getByText(/Chọn tất cả \(2 xe đang hiển thị\)/i)).toBeInTheDocument();
    expect(vehicleApi.getPaged).toHaveBeenCalledWith(
      expect.objectContaining({
        pageIndex: 1,
        pageSize: 20,
        isShared: true,
        isActive: true,
      })
    );
  });

  it('allows toggling single vehicle and toggling select all', async () => {
    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={vi.fn()}
      />
    );

    await waitFor(() => {
      expect(screen.getByText('29A-11111')).toBeInTheDocument();
    });

    // Click single vehicle
    fireEvent.click(screen.getByText('29A-11111'));

    // Click Select All
    const selectAllBtn = screen.getByText(/Chọn tất cả/i);
    fireEvent.click(selectAllBtn);
  });

  it('submits assignedVehicleIds when specific vehicles are selected', async () => {
    const handleSubmit = vi.fn().mockResolvedValue(undefined);

    renderWithClient(
      <GateRouteFormDialog
        open={true}
        onOpenChange={() => {}}
        onSubmit={handleSubmit}
        initialData={
          {
            id: 'route-1',
            routeCode: 'ROUTE-01',
            routeName: 'Tuyến Nhà Máy A -> B',
            isClosedLoop: true,
            isActive: true,
            gateSteps: [
              {
                gateId: 'gate-1',
                stepIndex: 1,
                maxTravelMinutes: 15,
                maxStayMinutes: 30,
              },
            ],
          } as any
        }
      />
    );

    await waitFor(() => {
      expect(screen.getByText('29A-11111')).toBeInTheDocument();
    });

    // Select vehicle 1
    fireEvent.click(screen.getByText('29A-11111'));

    // Submit form
    const submitBtn = screen.getByRole('button', { name: /Cập nhật/i });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(handleSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          routeCode: 'ROUTE-01',
          applyToAllSharedVehicles: false,
          assignedVehicleIds: ['v-1'],
        })
      );
    });
  });
});
