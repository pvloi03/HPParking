import type { PaginationQuery } from './masterData';

export const CardTargetType = {
  Person: 1,
  Vehicle: 2,
} as const;
export type CardTargetType = (typeof CardTargetType)[keyof typeof CardTargetType];

export const CardStatus = {
  Available: 0,
  InUse: 1,
  Locked: 2,
  Lost: 3,
} as const;
export type CardStatus = (typeof CardStatus)[keyof typeof CardStatus];

/**
 * Chuẩn hóa mã thẻ 10 chữ số (PadLeft 10 ký tự 0)
 */
export function normalizeCardCode(cardCode: string): string {
  if (!cardCode) return '';
  const digitsOnly = cardCode.replace(/\D/g, '');
  if (!digitsOnly) return '';
  return digitsOnly.length >= 10 ? digitsOnly : digitsOnly.padStart(10, '0');
}

export interface CardDto {
  id: string;
  cardNumber: string;
  targetType: CardTargetType;
  clientId?: string;
  clientName?: string;
  vehicleId?: string;
  plateNumber?: string;
  status: CardStatus;
  isActive?: boolean;
  note?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CardFilterQuery extends PaginationQuery {
  search?: string;
  targetType?: CardTargetType;
  status?: CardStatus;
}

export interface CreateCardRequest {
  cardNumber: string;
  targetType: CardTargetType;
  clientId?: string;
  vehicleId?: string;
  status?: CardStatus;
  note?: string;
}

export interface UpdateCardRequest {
  targetType: CardTargetType;
  clientId?: string;
  vehicleId?: string;
  status?: CardStatus;
  note?: string;
}
