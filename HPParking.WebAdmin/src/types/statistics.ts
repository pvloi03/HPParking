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

export interface UnitDistributionItemDto {
  companyId: string | null;
  companyName: string | null;
  departmentId: string | null;
  departmentName: string | null;
  clientCount: number;
  vehicleCount: number;
  gateCount: number;
  laneCount: number;
}

export interface DistributionStatisticsDto {
  totalFilteredClients: number;
  totalFilteredVehicles: number;
  totalFilteredGates: number;
  totalFilteredLanes: number;
  items: UnitDistributionItemDto[] | null;
}

export interface TrafficSummaryItemDto {
  personId: string | null;
  fullName: string | null;
  plateNumber: string | null;
  vehicleType: string | null;
  totalEntries: number;
  totalExits: number;
}
