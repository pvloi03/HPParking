import type { PaginationQuery } from './masterData';

export interface RouteGateStep {
  stepIndex: number;
  gateId: string;
  gateCode?: string;
  gateName?: string;
  maxTravelMinutes: number;
  maxStayMinutes: number;
}

export interface GateRouteDto {
  id: string;
  routeCode: string;
  routeName: string;
  description: string;
  gateSteps: RouteGateStep[];
  isClosedLoop: boolean;
  alertEmails: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface GateRouteFilterQuery extends PaginationQuery {
  search?: string;
  isActive?: boolean;
}

export interface CreateGateRouteRequest {
  routeCode: string;
  routeName: string;
  description?: string;
  gateSteps: RouteGateStep[];
  isClosedLoop: boolean;
  alertEmails?: string[];
  isActive: boolean;
}

export interface UpdateGateRouteRequest {
  routeCode: string;
  routeName: string;
  description?: string;
  gateSteps: RouteGateStep[];
  isClosedLoop: boolean;
  alertEmails?: string[];
  isActive: boolean;
}
