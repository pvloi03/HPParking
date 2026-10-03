import { SessionsManager } from '@/components/parkingSessions/SessionsManager';
import { LaneTargetType } from '@/types/infrastructure';

export function VehicleSessionsPage() {
  return <SessionsManager targetType={LaneTargetType.Vehicle} />;
}
