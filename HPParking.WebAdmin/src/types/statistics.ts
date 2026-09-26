import type { ClientType } from './client';
import type { VehicleType } from './vehicle';

/**
 * DTO tổng hợp các chỉ số KPIs vận hành mới nhất trên Dashboard (DashboardStatisticsDto.cs)
 */
export interface DashboardStatisticsDto {
  totalClients: number;
  activeClients: number;
  clientsByType: Record<string, number>;
  clientsWithFaceId: number;
  faceIdSyncRatePercentage: number;
  totalVehicles: number;
  activeVehicles: number;
  vehiclesByType: Record<string, number>;
  activeParkingSessions: number;
  totalGates: number;
  totalLanes: number;
  activeLanes: number;
}

/**
 * Tham số lọc báo cáo tổng hợp lưu lượng lượt ra vào theo từng người và xe (TrafficSummaryFilterQuery.cs)
 */
export interface TrafficSummaryFilterQuery {
  fromDate?: string;
  toDate?: string;
  companyId?: string;
  departmentId?: string;
  contractorId?: string;
  clientType?: ClientType;
  vehicleType?: VehicleType;
  plateNumber?: string;
  searchTerm?: string;
}

/**
 * Bản ghi tổng hợp lưu lượng lượt ra vào của từng người và phương tiện (TrafficSummaryItemDto.cs)
 */
export interface TrafficSummaryItemDto {
  personId?: string;
  clientCode: string;
  clientName: string;
  clientType?: ClientType;
  clientTypeName: string;
  companyName: string;
  departmentName: string;
  plateNumber: string;
  vehicleType: VehicleType;
  inCount: number;
  outCount: number;
  completedCount: number;
  activeCount: number;
  isInParking: boolean;
}
