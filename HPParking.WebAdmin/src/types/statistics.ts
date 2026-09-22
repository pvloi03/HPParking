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

export interface TrafficSummaryItemDto {
  personId: string | null;
  fullName: string | null;
  plateNumber: string | null;
  vehicleType: string | null;
  totalEntries: number;
  totalExits: number;
}
